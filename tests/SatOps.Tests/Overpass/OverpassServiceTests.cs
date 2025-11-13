using Moq;
using Xunit;
using FluentAssertions;
using SatOps.Modules.Overpass;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;

namespace SatOps.Tests
{
    public class OverpassServiceTests
    {
        private readonly IOverpassService _overpassService;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IGroundStationService> _mockGroundStationService;
        private readonly Mock<IOverpassRepository> _mockOverpassRepository;
        private const double Tolerance = 0.0001;

        public OverpassServiceTests()
        {
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGroundStationService = new Mock<IGroundStationService>();
            _mockOverpassRepository = new Mock<IOverpassRepository>();

            _overpassService = new OverpassService(
                _mockSatelliteService.Object,
                _mockGroundStationService.Object,
                _mockOverpassRepository.Object
            );
        }

        [Fact]
        public async Task GetStoredOverpassAsync_ShouldReturnEntity_WhenIdExists()
        {
            // Arrange
            var existingId = 1;
            var expectedOverpass = new Entity { Id = existingId };
            _mockOverpassRepository.Setup(repo => repo.GetByIdReadOnlyAsync(existingId))
                .ReturnsAsync(expectedOverpass);

            // Act
            var result = await _overpassService.GetStoredOverpassAsync(existingId);

            // Assert
            result.Should().BeEquivalentTo(expectedOverpass);
        }

        [Fact]
        public async Task GetStoredOverpassAsync_ShouldReturnNull_WhenIdDoesNotExist()
        {
            // Arrange
            var nonExistingId = 999;
            _mockOverpassRepository.Setup(repo => repo.GetByIdReadOnlyAsync(nonExistingId))
                .ReturnsAsync((Entity?)null);

            // Act
            var result = await _overpassService.GetStoredOverpassAsync(nonExistingId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FindOrCreateOverpassForFlightPlanAsync_ShouldCreateNewOverpass_WhenNoneExists()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10)
            };
            var flightPlanId = 101;
            var toleranceMinutes = 15;

            _mockOverpassRepository.Setup(repo => repo.FindOverpassInTimeWindowAsync(
                overpassWindowDto.SatelliteId,
                overpassWindowDto.GroundStationId,
                overpassWindowDto.StartTime,
                overpassWindowDto.EndTime,
                toleranceMinutes
            )).ReturnsAsync((Entity?)null);

            _mockOverpassRepository.Setup(repo => repo.AddAsync(It.IsAny<Entity>()))
                .ReturnsAsync((Entity e) => e);

            // Act
            var (success, result, message) = await _overpassService.FindOrCreateOverpassForFlightPlanAsync(
                overpassWindowDto,
                flightPlanId,
                toleranceMinutes
            );

            // Assert
            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.FlightPlanId.Should().Be(flightPlanId);
            message.Should().Be("Overpass created and assigned successfully.");
            _mockOverpassRepository.Verify(repo => repo.AddAsync(It.IsAny<Entity>()), Times.Once);
        }

        [Fact]
        public async Task FindOrCreateOverpassForFlightPlanAsync_ShouldFail_WhenOverpassAlreadyExists()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10)
            };
            var flightPlanId = 101;
            var toleranceMinutes = 15;

            var existingFlightPlan = new Modules.FlightPlan.FlightPlan { Id = 99, Name = "Existing Plan" };
            var existingOverpass = new Entity
            {
                Id = 1,
                FlightPlanId = existingFlightPlan.Id,
                FlightPlan = existingFlightPlan
            };

            _mockOverpassRepository.Setup(repo => repo.FindOverpassInTimeWindowAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()
            )).ReturnsAsync(existingOverpass);

            // Act
            var (success, result, message) = await _overpassService.FindOrCreateOverpassForFlightPlanAsync(
                overpassWindowDto,
                flightPlanId,
                toleranceMinutes
            );

            // Assert
            success.Should().BeFalse();
            result.Should().BeNull();
            message.Should().Contain("An overpass is already assigned to flight plan 'Existing Plan'");
            _mockOverpassRepository.Verify(repo => repo.AddAsync(It.IsAny<Entity>()), Times.Never);
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ShouldThrowArgumentException_WhenSatelliteNotFound()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());

            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync((Satellite?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ShouldThrowArgumentException_WhenGroundstationNotFound()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };
            var satellite = new Satellite { Id = requestDto.SatelliteId, Name = "TestSat" };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());

            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync((GroundStation?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Theory]
        [MemberData(nameof(TleMissingData))]
        public async Task CalculateOverpassesAsync_ShouldThrowInvalidOperationException_WhenTLEDataIsMissing(string tleLine1, string tleLine2)
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };
            var satellite = new Satellite { Id = 1, Name = "TestSat", TleLine1 = tleLine1, TleLine2 = tleLine2 };
            var groundStation = new GroundStation { Id = 1, Name = "TestGS" };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());

            _mockSatelliteService.Setup(s => s.GetAsync(1)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(1)).ReturnsAsync(groundStation);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _overpassService.CalculateOverpassesAsync(requestDto));
        }
        [Fact]
        public async Task CalculateOverpassesAsync_ReturnsCalculatedOverpasses_WhenValidRequest()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = new DateTime(2025, 10, 27, 12, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc),
            };
            var satellite = new Satellite
            {
                Id = requestDto.SatelliteId,
                Name = "TestSat",
                TleLine1 = "1 25544U 98067A   21275.12345678  .00001234  00000-0  12345-6 0  9999",
                TleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616",
            };
            var groundStation = new GroundStation
            {
                Id = requestDto.GroundStationId,
                Name = "TestGS",
                Location = new Location
                {
                    Latitude = 40.0,
                    Longitude = -74.0,
                    Altitude = 0.0
                }
            };
            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 540.0,
                    StartAzimuth = 193.56243709717364,
                    EndAzimuth = 64.04225383927546,
                    MaxElevation = 17.92577512997181,
                    StartTime = new DateTime(2025, 10, 27, 16, 46, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 16, 56, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 16, 50, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 245.44239459556508,
                    EndAzimuth = 50.80675380708287,
                    MaxElevation = 48.66778766406786,
                    StartTime = new DateTime(2025, 10, 27, 18, 22, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 33, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 27, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 540.0,
                    StartAzimuth = 286.7926496754286,
                    EndAzimuth = 51.7566488837168,
                    MaxElevation = 14.256324958843932,
                    StartTime = new DateTime(2025, 10, 27, 20, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 20, 10, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 20, 4, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 480.0,
                    StartAzimuth = 310.72385802357894,
                    EndAzimuth = 65.14455142181063,
                    MaxElevation = 11.942598228372539,
                    StartTime = new DateTime(2025, 10, 27, 21, 38, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 21, 47, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 21, 42, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 313.5257429866042,
                    EndAzimuth = 103.0272058127901,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 15, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 26, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 299.9138934245744,
                    EndAzimuth = 144.4620350778898,
                    MaxElevation = 37.78527400884674,
                    StartTime = new DateTime(2025, 10, 28, 0, 52, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 1, 3, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 57, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 240.0,
                    StartAzimuth = 258.4486832681079,
                    EndAzimuth = 203.98277826626062,
                    MaxElevation = 1.9340954318687928,
                    StartTime = new DateTime(2025, 10, 28, 2, 31, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 2, 36, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 2, 33, 0, DateTimeKind.Utc),
                }
            };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync(groundStation);

            // Act
            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            // Assert
            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)
                ).WhenTypeIs<double>()
            );
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ReturnsCalculatedOverpasses_WithMinimumElevation()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = new DateTime(2025, 10, 27, 12, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc),
                MinimumElevation = 20
            };
            var satellite = new Satellite
            {
                Id = requestDto.SatelliteId,
                Name = "TestSat",
                TleLine1 = "1 25544U 98067A   21275.12345678  .00001234  00000-0  12345-6 0  9999",
                TleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616",
            };
            var groundStation = new GroundStation
            {
                Id = requestDto.GroundStationId,
                Name = "TestGS",
                Location = new Location
                {
                    Latitude = 40.0,
                    Longitude = -74.0,
                    Altitude = 0.0
                }
            };
            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 240.0,
                    StartAzimuth = 259.41504317273916,
                    EndAzimuth = 42.55275439816543,
                    MaxElevation = 48.66778766406786,
                    StartTime = new DateTime(2025, 10, 27, 18, 25, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 30, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 27, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 120.0,
                    StartAzimuth = 355.30215840543156,
                    EndAzimuth = 77.4871552132818,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 19, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 22, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 120.0,
                    StartAzimuth = 260.37950007403276,
                    EndAzimuth = 162.050781306894,
                    MaxElevation = 37.78527400884674,
                    StartTime = new DateTime(2025, 10, 28, 0, 56, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 0, 59, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 57, 0, DateTimeKind.Utc),
                }
            };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId))
                .ReturnsAsync(groundStation);

            // Act
            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            // Assert
            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)
                ).WhenTypeIs<double>()
            );
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ReturnsCalculatedOverpasses_WithMaxResults()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = new DateTime(2025, 10, 27, 12, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc),
                MaxResults = 2
            };
            var satellite = new Satellite
            {
                Id = requestDto.SatelliteId,
                Name = "TestSat",
                TleLine1 = "1 25544U 98067A   21275.12345678  .00001234  00000-0  12345-6 0  9999",
                TleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616",
            };
            var groundStation = new GroundStation
            {
                Id = requestDto.GroundStationId,
                Name = "TestGS",
                Location = new Location
                {
                    Latitude = 40.0,
                    Longitude = -74.0,
                    Altitude = 0.0
                }
            };
            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 540.0,
                    StartAzimuth = 193.56243709717364,
                    EndAzimuth = 64.04225383927546,
                    MaxElevation = 17.92577512997181,
                    StartTime = new DateTime(2025, 10, 27, 16, 46, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 16, 56, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 16, 50, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 245.44239459556508,
                    EndAzimuth = 50.80675380708287,
                    MaxElevation = 48.66778766406786,
                    StartTime = new DateTime(2025, 10, 27, 18, 22, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 33, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 27, 0, DateTimeKind.Utc),
                }
            };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId))
                .ReturnsAsync(groundStation);

            // Act
            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            // Assert
            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)
                ).WhenTypeIs<double>()
            );
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ReturnsCalculatedOverpasses_WithMinimumDuration()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = new DateTime(2025, 10, 27, 12, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc),
                MinimumDurationSeconds = 600
            };
            var satellite = new Satellite
            {
                Id = requestDto.SatelliteId,
                Name = "TestSat",
                TleLine1 = "1 25544U 98067A   21275.12345678  .00001234  00000-0  12345-6 0  9999",
                TleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616",
            };
            var groundStation = new GroundStation
            {
                Id = requestDto.GroundStationId,
                Name = "TestGS",
                Location = new Location
                {
                    Latitude = 40.0,
                    Longitude = -74.0,
                    Altitude = 0.0
                }
            };
            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 245.44239459556508,
                    EndAzimuth = 50.80675380708287,
                    MaxElevation = 48.66778766406786,
                    StartTime = new DateTime(2025, 10, 27, 18, 22, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 33, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 27, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 313.5257429866042,
                    EndAzimuth = 103.0272058127901,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 15, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 26, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 0, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 600.0,
                    StartAzimuth = 299.9138934245744,
                    EndAzimuth = 144.4620350778898,
                    MaxElevation = 37.78527400884674,
                    StartTime = new DateTime(2025, 10, 28, 0, 52, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 1, 3, 0, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 57, 0, DateTimeKind.Utc),
                }
            };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId))
                .ReturnsAsync(groundStation);

            // Act
            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            // Assert
            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)
                ).WhenTypeIs<double>()
            );
        }

        public static IEnumerable<object?[]> TleMissingData =>
        new List<object?[]>
        {
            new object?[] { "", "validTleLine2" },
            new object?[] { "validTleLine1", "" },
            new object?[] { null, "validTleLine2" },
            new object?[] { "validTleLine1", null }
        };
    }
}