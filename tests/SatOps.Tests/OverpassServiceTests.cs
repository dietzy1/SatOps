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
        public async Task ReturnValidEntity_WhenGetStoredOverpassAsync_IsCalledWithExistingId()
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
        public async Task ReturnNull_WhenGetStoredOverpassAsync_IsCalledWithNonExistingId()
        {
            // Arrange
            var existingId = 1;
            var nonExistingId = 999;
            var expectedOverpass = new Entity { Id = existingId };
            _mockOverpassRepository.Setup(repo => repo.GetByIdReadOnlyAsync(existingId))
                .ReturnsAsync(expectedOverpass);

            // Act
            var result = await _overpassService.GetStoredOverpassAsync(nonExistingId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task StoreOverpassAsync_CallsRepositoryAddAsync_WithCorrectEntity()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10),
                MaxElevationTime = DateTime.UtcNow.AddMinutes(5),
                MaxElevation = 45.0,
                DurationSeconds = 600,
                StartAzimuth = 90.0,
                EndAzimuth = 270.0,
            };
            var tleLine1 = "1 25544U 98067A   20029.54791435  .00001264  00000-0  29621-4 0  9991";
            var tleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616";
            var tleUpdateTime = DateTime.UtcNow.AddDays(-1);
            var expectedEntity = new Entity
            {
                SatelliteId = overpassWindowDto.SatelliteId,
                GroundStationId = overpassWindowDto.GroundStationId,
                StartTime = overpassWindowDto.StartTime,
                EndTime = overpassWindowDto.EndTime,
                MaxElevationTime = overpassWindowDto.MaxElevationTime,
                MaxElevation = overpassWindowDto.MaxElevation,
                DurationSeconds = (int)overpassWindowDto.DurationSeconds,
                StartAzimuth = overpassWindowDto.StartAzimuth,
                EndAzimuth = overpassWindowDto.EndAzimuth,
                TleLine1 = tleLine1,
                TleLine2 = tleLine2,
                TleUpdateTime = tleUpdateTime
            };

            // Act
            var result = await _overpassService.StoreOverpassAsync(
                overpassWindowDto,
                tleLine1,
                tleLine2,
                tleUpdateTime
            );

            // Assert
            _mockOverpassRepository.Verify(repo => repo.AddAsync(It.Is<Entity>(e =>
                e.SatelliteId == expectedEntity.SatelliteId &&
                e.GroundStationId == expectedEntity.GroundStationId &&
                e.StartTime == expectedEntity.StartTime &&
                e.EndTime == expectedEntity.EndTime &&
                e.MaxElevationTime == expectedEntity.MaxElevationTime &&
                e.MaxElevation == expectedEntity.MaxElevation &&
                e.DurationSeconds == expectedEntity.DurationSeconds &&
                e.StartAzimuth == expectedEntity.StartAzimuth &&
                e.EndAzimuth == expectedEntity.EndAzimuth &&
                e.TleLine1 == expectedEntity.TleLine1 &&
                e.TleLine2 == expectedEntity.TleLine2 &&
                e.TleUpdateTime == expectedEntity.TleUpdateTime
            )), Times.Once);
        }

        [Fact]
        public async Task StoreOverpassAsync_ReturnsEntity_WithCorrectProperties()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10),
                MaxElevationTime = DateTime.UtcNow.AddMinutes(5),
                MaxElevation = 45.0,
                DurationSeconds = 600,
                StartAzimuth = 90.0,
                EndAzimuth = 270.0,
            };
            var tleLine1 = "1 25544U 98067A   20029.54791435  .00001264  00000-0  29621-4 0  9991";
            var tleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616";
            var tleUpdateTime = DateTime.UtcNow.AddDays(-1);
            var expectedEntity = new Entity
            {
                SatelliteId = overpassWindowDto.SatelliteId,
                GroundStationId = overpassWindowDto.GroundStationId,
                StartTime = overpassWindowDto.StartTime,
                EndTime = overpassWindowDto.EndTime,
                MaxElevationTime = overpassWindowDto.MaxElevationTime,
                MaxElevation = overpassWindowDto.MaxElevation,
                DurationSeconds = (int)overpassWindowDto.DurationSeconds,
                StartAzimuth = overpassWindowDto.StartAzimuth,
                EndAzimuth = overpassWindowDto.EndAzimuth,
                TleLine1 = tleLine1,
                TleLine2 = tleLine2,
                TleUpdateTime = tleUpdateTime
            };
            _mockOverpassRepository.Setup(repo => repo.AddAsync(It.IsAny<Entity>()))
                .ReturnsAsync((Entity e) => e);

            // Act
            var result = await _overpassService.StoreOverpassAsync(
                overpassWindowDto,
                tleLine1,
                tleLine2,
                tleUpdateTime
            );

            // Assert
            result.Should().BeEquivalentTo(expectedEntity);
        }

        [Fact]
        public async Task FindOrCreateOverpassAsync_ReturnsMatchingEntity_WhenOverPassExists()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10),
                MaxElevation = 45.0,
            };
            var existingEntity = new Entity
            {
                SatelliteId = overpassWindowDto.SatelliteId,
                GroundStationId = overpassWindowDto.GroundStationId,
                StartTime = overpassWindowDto.StartTime,
                EndTime = overpassWindowDto.EndTime,
                MaxElevation = overpassWindowDto.MaxElevation,
            };
            _mockOverpassRepository.Setup(repo => repo.FindExistingOverpassAsync(
                overpassWindowDto.SatelliteId,
                overpassWindowDto.GroundStationId,
                overpassWindowDto.StartTime,
                overpassWindowDto.EndTime,
                overpassWindowDto.MaxElevation
            )).ReturnsAsync(existingEntity);

            // Act
            var result = await _overpassService.FindOrCreateOverpassAsync(
                overpassWindowDto
            );

            // Assert
            result.Should().BeEquivalentTo(existingEntity);
        }

        [Fact]
        public async Task FindOrCreateOverpassAsync_CallsRepositoryAddAsync_WhenOverpassDoesNotExist()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10),
                MaxElevation = 45.0,
            };
            var entity = new Entity
            {
                SatelliteId = overpassWindowDto.SatelliteId,
                GroundStationId = overpassWindowDto.GroundStationId,
                StartTime = overpassWindowDto.StartTime,
                EndTime = overpassWindowDto.EndTime,
                MaxElevation = overpassWindowDto.MaxElevation,
            };

            // Act
            await _overpassService.FindOrCreateOverpassAsync(
                overpassWindowDto
            );

            // Assert
            _mockOverpassRepository.Verify(repo => repo.AddAsync(It.Is<Entity>(e =>
                e.SatelliteId == entity.SatelliteId &&
                e.GroundStationId == entity.GroundStationId &&
                e.StartTime == entity.StartTime &&
                e.EndTime == entity.EndTime &&
                e.MaxElevation == entity.MaxElevation
            )), Times.Once);
        }

        [Fact]
        public async Task FindOrCreateOverpassAsync_ReturnsNewEntity_WhenOverpassDoesNotExist()
        {
            // Arrange
            var overpassWindowDto = new OverpassWindowDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(10),
                MaxElevation = 45.0,
            };
            var expectedEntity = new Entity
            {
                SatelliteId = overpassWindowDto.SatelliteId,
                GroundStationId = overpassWindowDto.GroundStationId,
                StartTime = overpassWindowDto.StartTime,
                EndTime = overpassWindowDto.EndTime,
                MaxElevation = overpassWindowDto.MaxElevation,
            };
            _mockOverpassRepository.Setup(repo => repo.AddAsync(It.IsAny<Entity>()))
                .ReturnsAsync((Entity e) => e);

            // Act
            var result = await _overpassService.FindOrCreateOverpassAsync(
                overpassWindowDto
            );

            // Assert
            result.Should().BeEquivalentTo(expectedEntity);
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ThrowsArgumentException_WhenSatelliteNotFound()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
            };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync((Satellite?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ThrowsArgumentException_WhenGroundstationNotFound()
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
            };
            var satellite = new Satellite { Id = requestDto.SatelliteId, Name = "TestSat" };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId))
                .ReturnsAsync((GroundStation?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Theory]
        [InlineData("", "validTleLine2")]
        [InlineData("validTleLine1", "")]
        [InlineData("", "")]
        public async Task CalculateOverpassesAsync_ThrowsInvalidOperationException_WhenTLEDataIsMissing(string tleLine1, string tleLine2)
        {
            // Arrange
            var requestDto = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
            };
            var satellite = new Satellite
            {
                Id = requestDto.SatelliteId,
                Name = "TestSat",
                TleLine1 = tleLine1,
                TleLine2 = tleLine2,
            };
            var groundStation = new GroundStation { Id = requestDto.GroundStationId, Name = "TestGS" };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId))
                .ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId))
                .ReturnsAsync(groundStation);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _overpassService.CalculateOverpassesAsync(requestDto));
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
            )).ReturnsAsync(new List<Entity>());
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
            )).ReturnsAsync(new List<Entity>());
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
                new OverpassWindowDto
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
            )).ReturnsAsync(new List<Entity>());
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
    }
}