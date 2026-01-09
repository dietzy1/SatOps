using Moq;
using Xunit;
using FluentAssertions;
using SatOps.Modules.Overpass;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;

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
        public async Task GetStoredOverpassAsync_ShouldReturnEntity_WhenIdExists()
        {
            var existingId = 1;
            var expectedOverpass = new Entity { Id = existingId };
            _mockOverpassRepository.Setup(repo => repo.GetByIdReadOnlyAsync(existingId))
                .ReturnsAsync(expectedOverpass);

            var result = await _overpassService.GetStoredOverpassAsync(existingId);
            result.Should().BeEquivalentTo(expectedOverpass);
        }

        [Fact]
        public async Task GetStoredOverpassAsync_ShouldReturnNull_WhenIdDoesNotExist()
        {
            var nonExistingId = 999;
            _mockOverpassRepository.Setup(repo => repo.GetByIdReadOnlyAsync(nonExistingId))
                .ReturnsAsync((Entity?)null);

            var result = await _overpassService.GetStoredOverpassAsync(nonExistingId);
            result.Should().BeNull();
        }

        [Fact]
        public async Task FindOrCreateOverpassForFlightPlanAsync_ShouldCreateNewOverpass_WhenNoneExists()
        {
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

            var (success, result, message) = await _overpassService.FindOrCreateOverpassForFlightPlanAsync(
                overpassWindowDto,
                flightPlanId,
                toleranceMinutes
            );

            success.Should().BeTrue();
            result.Should().NotBeNull();
            result!.FlightPlanId.Should().Be(flightPlanId);
            message.Should().Be("Overpass created and assigned successfully.");
        }

        [Fact]
        public async Task FindOrCreateOverpassForFlightPlanAsync_ShouldFail_WhenOverpassAlreadyExists()
        {
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

            var (success, result, message) = await _overpassService.FindOrCreateOverpassForFlightPlanAsync(
                overpassWindowDto,
                flightPlanId,
                toleranceMinutes
            );

            success.Should().BeFalse();
            result.Should().BeNull();
            message.Should().Contain("An overpass is already assigned to flight plan 'Existing Plan'");
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ShouldThrowArgumentException_WhenSatelliteNotFound()
        {
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync((SatelliteEntity?)null);

            await Assert.ThrowsAsync<ArgumentException>(() => _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Fact]
        public async Task CalculateOverpassesAsync_ShouldThrowArgumentException_WhenGroundstationNotFound()
        {
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };
            var satellite = new SatelliteEntity { Id = requestDto.SatelliteId, Name = "TestSat" };
            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync((GroundStation?)null);

            await Assert.ThrowsAsync<ArgumentException>(() => _overpassService.CalculateOverpassesAsync(requestDto));
        }

        [Theory]
        [MemberData(nameof(TleMissingData))]
        public async Task CalculateOverpassesAsync_ShouldThrowInvalidOperationException_WhenTLEDataIsMissing(string tleLine1, string tleLine2)
        {
            var requestDto = new OverpassWindowsCalculationRequestDto { SatelliteId = 1, GroundStationId = 1 };
            var satellite = new SatelliteEntity { Id = 1, Name = "TestSat", TleLine1 = tleLine1, TleLine2 = tleLine2 };
            var groundStation = new GroundStation { Id = 1, Name = "TestGS" };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync(new List<Entity>());
            _mockSatelliteService.Setup(s => s.GetAsync(1)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(1)).ReturnsAsync(groundStation);

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
            var satellite = GetTestSatellite(requestDto.SatelliteId);
            var groundStation = GetTestGroundStation(requestDto.GroundStationId);


            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 589.0,
                    StartAzimuth = 195.99849292826318,
                    EndAzimuth = 66.78450563781013,
                    MaxElevation = 18.31918531001896,
                    StartTime = new DateTime(2025, 10, 27, 16, 45, 31, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 16, 55, 20, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 16, 50, 25, 400, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 649.0,
                    StartAzimuth = 244.60454080200276,
                    EndAzimuth = 49.76445647311203,
                    MaxElevation = 48.68301609025784,
                    StartTime = new DateTime(2025, 10, 27, 18, 21, 34, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 32, 23, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 26, 58, 400, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 574.0,
                    StartAzimuth = 284.5047675922203,
                    EndAzimuth = 47.6105104621574,
                    MaxElevation = 14.492096705347953,
                    StartTime = new DateTime(2025, 10, 27, 19, 59, 37, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 20, 09, 11, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 20, 04, 24, 100, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 550.0,
                    StartAzimuth = 308.99681433704643,
                    EndAzimuth = 64.52301104322532,
                    MaxElevation = 12.066939299307071,
                    StartTime = new DateTime(2025, 10, 27, 21, 37, 44, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 21, 46, 54, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 21, 42, 19, 800, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 626.0,
                    StartAzimuth = 312.67944176345304,
                    EndAzimuth = 100.5411623701663,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 14, 46, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 25, 12, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 0, 100, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 633.0,
                    StartAzimuth = 300.9333400301767,
                    EndAzimuth = 146.18250543340628,
                    MaxElevation = 38.00350685637695,
                    StartTime = new DateTime(2025, 10, 28, 0, 51, 34, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 1, 02, 07, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 56, 51, 700, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 273.0,
                    StartAzimuth = 263.01011714326654,
                    EndAzimuth = 212.277304528398,
                    MaxElevation = 1.947414159709326,
                    StartTime = new DateTime(2025, 10, 28, 2, 30, 33, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 2, 35, 06, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 2, 32, 49, 200, DateTimeKind.Utc),
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
            var satellite = GetTestSatellite(requestDto.SatelliteId);
            var groundStation = GetTestGroundStation(requestDto.GroundStationId);

            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 246.0,
                    StartAzimuth = 258.5628312283407,
                    EndAzimuth = 35.549444013450035,
                    MaxElevation = 48.68301609025784,
                    StartTime = new DateTime(2025, 10, 27, 18, 24, 55, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 29, 01, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 26, 58, 400, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 174.0,
                    StartAzimuth = 345.1307007494323,
                    EndAzimuth = 68.11540811049373,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 18, 33, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 21, 27, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 00, 100, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 221.0,
                    StartAzimuth = 281.5068101253377,
                    EndAzimuth = 165.80440850256264,
                    MaxElevation = 38.00350685637695,
                    StartTime = new DateTime(2025, 10, 28, 0, 55, 01, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 0, 58, 42, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 56, 51, 700, DateTimeKind.Utc),
                }
            };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync(groundStation);

            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)).WhenTypeIs<double>()
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
            var satellite = GetTestSatellite(requestDto.SatelliteId);
            var groundStation = GetTestGroundStation(requestDto.GroundStationId);

            // Matches the first two results of ValidRequest
            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 589.0,
                    StartAzimuth = 195.99849292826318,
                    EndAzimuth = 66.78450563781013,
                    MaxElevation = 18.31918531001896,
                    StartTime = new DateTime(2025, 10, 27, 16, 45, 31, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 16, 55, 20, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 16, 50, 25, 400, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 649.0,
                    StartAzimuth = 244.60454080200276,
                    EndAzimuth = 49.76445647311203,
                    MaxElevation = 48.68301609025784,
                    StartTime = new DateTime(2025, 10, 27, 18, 21, 34, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 32, 23, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 26, 58, 400, DateTimeKind.Utc),
                }
            };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync(groundStation);

            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)).WhenTypeIs<double>()
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
            var satellite = GetTestSatellite(requestDto.SatelliteId);
            var groundStation = GetTestGroundStation(requestDto.GroundStationId);


            var expectedOverpasses = new List<OverpassWindowDto>
            {
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 649.0,
                    StartAzimuth = 244.60454080200276,
                    EndAzimuth = 49.76445647311203,
                    MaxElevation = 48.68301609025784,
                    StartTime = new DateTime(2025, 10, 27, 18, 21, 34, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 18, 32, 23, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 18, 26, 58, 400, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 626.0,
                    StartAzimuth = 312.67944176345304,
                    EndAzimuth = 100.5411623701663,
                    MaxElevation = 27.593081105655784,
                    StartTime = new DateTime(2025, 10, 27, 23, 14, 46, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 27, 23, 25, 12, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 27, 23, 20, 0, 100, DateTimeKind.Utc),
                },
                new()
                {
                    SatelliteId = requestDto.SatelliteId,
                    SatelliteName = "TestSat",
                    GroundStationId = requestDto.GroundStationId,
                    GroundStationName = "TestGS",
                    DurationSeconds = 633.0,
                    StartAzimuth = 300.9333400301767,
                    EndAzimuth = 146.18250543340628,
                    MaxElevation = 38.00350685637695,
                    StartTime = new DateTime(2025, 10, 28, 0, 51, 34, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 28, 1, 02, 07, DateTimeKind.Utc),
                    MaxElevationTime = new DateTime(2025, 10, 28, 0, 56, 51, 700, DateTimeKind.Utc),
                }
            };

            _mockOverpassRepository.Setup(repo => repo.FindStoredOverpassesInTimeRange(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()
            )).ReturnsAsync([]);
            _mockSatelliteService.Setup(s => s.GetAsync(requestDto.SatelliteId)).ReturnsAsync(satellite);
            _mockGroundStationService.Setup(s => s.GetAsync(requestDto.GroundStationId)).ReturnsAsync(groundStation);

            var result = await _overpassService.CalculateOverpassesAsync(requestDto);

            result.Should().BeEquivalentTo(expectedOverpasses, options =>
                options.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001)).WhenTypeIs<double>()
            );
        }

        // Helpers

        private static SatelliteEntity GetTestSatellite(int id) => new()
        {
            Id = id,
            Name = "TestSat",
            TleLine1 = "1 25544U 98067A   21275.12345678  .00001234  00000-0  12345-6 0  9999",
            TleLine2 = "2 25544  51.6432 348.7416 0007417  85.4084 274.7553 15.49112339210616",
        };

        private static GroundStation GetTestGroundStation(int id) => new()
        {
            Id = id,
            Name = "TestGS",
            Location = new Location { Latitude = 40.0, Longitude = -74.0, Altitude = 0.0 }
        };

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