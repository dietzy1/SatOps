using Moq;
using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.User;
using SatOps.Modules.Satellite;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using FlightPlanEntity = SatOps.Modules.FlightPlan.FlightPlan;
using Microsoft.Extensions.Options;
using SatOps.Configuration;
using SatOps.Modules.FlightPlan.Commands;
using SGPdotNET.CoordinateSystem;

namespace SatOps.Tests
{
    public class FlightPlanServiceTests
    {
        private readonly Mock<IFlightPlanRepository> _mockFlightPlanRepo;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IGroundStationService> _mockGroundStationService;
        private readonly Mock<ICurrentUserProvider> _mockCurrentUserProvider;
        private readonly Mock<IOptions<ImagingCalculationOptions>> _mockImagingOptions;
        private readonly Mock<IOverpassService> _mockOverpassService;
        private readonly Mock<IImagingCalculation> _mockImagingCalculation;

        private readonly FlightPlanService _sut;

        public FlightPlanServiceTests()
        {
            _mockFlightPlanRepo = new Mock<IFlightPlanRepository>();
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGroundStationService = new Mock<IGroundStationService>();
            _mockOverpassService = new Mock<IOverpassService>();
            _mockImagingCalculation = new Mock<IImagingCalculation>();
            _mockCurrentUserProvider = new Mock<ICurrentUserProvider>();

            _mockImagingOptions = new Mock<IOptions<ImagingCalculationOptions>>();
            _mockImagingOptions.Setup(o => o.Value).Returns(new ImagingCalculationOptions());

            _sut = new FlightPlanService(
                _mockFlightPlanRepo.Object,
                _mockSatelliteService.Object,
                _mockGroundStationService.Object,
                _mockOverpassService.Object,
                _mockImagingCalculation.Object,
                _mockCurrentUserProvider.Object,
                _mockImagingOptions.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WhenUserIsAuthenticated_AssignsCurrentUserIdToCreatedById()
        {
            // Arrange
            var testUserId = 123;
            var createDto = new CreateFlightPlanDto
            {
                Name = "Test Plan",
                GsId = 1,
                SatId = 1,
                Commands = [new Modules.FlightPlan.Commands.TriggerPipelineCommand { Mode = 1, ExecutionTime = System.DateTime.UtcNow }]
            };

            // Setup mocks
            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(testUserId);
            _mockGroundStationService.Setup(s => s.GetAsync(createDto.GsId)).ReturnsAsync(new GroundStation());
            _mockSatelliteService.Setup(s => s.GetAsync(createDto.SatId)).ReturnsAsync(new SatelliteEntity());
            _mockFlightPlanRepo.Setup(r => r.AddAsync(It.IsAny<FlightPlanEntity>()))
                .ReturnsAsync((FlightPlanEntity fp) => fp);

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.CreatedById.Should().Be(testUserId);
            _mockFlightPlanRepo.Verify(r => r.AddAsync(It.Is<FlightPlanEntity>(fp => fp.CreatedById == testUserId)), Times.Once);
        }

        [Theory]
        [InlineData("APPROVED")]
        [InlineData("REJECTED")]
        public async Task ApproveOrRejectAsync_WhenPlanIsDraft_UpdatesStatusAndAssignsCurrentUserAsApprover(string targetStatus)
        {
            // Arrange
            var testUserId = 456;
            var planId = 1;
            var draftPlan = new FlightPlanEntity { Id = planId, Status = FlightPlanStatus.Draft };
            draftPlan.SetCommands([new Modules.FlightPlan.Commands.TriggerPipelineCommand { Mode = 1, ExecutionTime = System.DateTime.UtcNow }]); // Ensure commands are valid

            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(testUserId);
            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(draftPlan);

            // Act
            var (success, _) = await _sut.ApproveOrRejectAsync(planId, targetStatus);

            // Assert
            success.Should().BeTrue();
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.Is<FlightPlanEntity>(p =>
                p.Id == planId &&
                p.Status == FlightPlanStatusExtensions.FromScreamCase(targetStatus) &&
                p.ApprovedById == testUserId
            )), Times.Once);
        }

        [Fact]
        public async Task ApproveOrRejectAsync_WhenPlanIsNotDraft_ReturnsFailure()
        {
            // Arrange
            var planId = 1;
            var approvedPlan = new FlightPlanEntity { Id = planId, Status = FlightPlanStatus.Approved };

            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(approvedPlan);
            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(123);

            // Act
            var (success, message) = await _sut.ApproveOrRejectAsync(planId, "APPROVED");

            // Assert
            success.Should().BeFalse();
            message.Should().Be("Cannot modify a plan that has already been approved.");
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.IsAny<FlightPlanEntity>()), Times.Never);
        }


        [Fact]
        public async Task UpdateFlightPlanStatusAsync_WhenPlanExists_UpdatesStatusAndFailureReason()
        {
            // Arrange
            var planId = 1;
            var plan = new FlightPlanEntity { Id = planId, Status = FlightPlanStatus.AssignedToOverpass };
            var newStatus = FlightPlanStatus.Failed;
            var reason = "Ground station offline";

            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(plan);

            // Act
            await _sut.UpdateFlightPlanStatusAsync(planId, newStatus, reason);

            // Assert
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.Is<FlightPlanEntity>(p =>
                p.Id == planId &&
                p.Status == newStatus &&
                p.FailureReason == reason
            )), Times.Once);
        }

        [Fact]
        public async Task GetPlansReadyForTransmissionAsync_CallsRepositoryWithCorrectHorizon()
        {
            // Arrange
            var lookahead = TimeSpan.FromMinutes(5);
            var expectedHorizon = DateTime.UtcNow.Add(lookahead);

            _mockFlightPlanRepo.Setup(r => r.GetPlansReadyForTransmissionAsync(It.IsAny<DateTime>()))
                .ReturnsAsync([]);

            // Act
            await _sut.GetPlansReadyForTransmissionAsync(lookahead);

            // Assert
            // Verify that the repository was called with a DateTime that is very close to what we expect
            _mockFlightPlanRepo.Verify(r => r.GetPlansReadyForTransmissionAsync(
                It.Is<DateTime>(dt => dt > expectedHorizon.AddSeconds(-1) && dt < expectedHorizon.AddSeconds(1))
            ), Times.Once);
        }

        [Fact]
        public async Task AssignOverpassAsync_WhenCommandExecutesDuringOverpass_ReturnsChronologyError()
        {
            // Arrange (Time Travel Scenario)
            var planId = 1;
            var satId = 100;
            var gsId = 5;

            // FIX: Use valid TLE strings so SGP4 library doesn't crash
            var validTle1 = "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992";
            var validTle2 = "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673";

            // The Overpass (Upload Window) is 10:00 -> 10:10 (Future)
            var overpassStart = DateTime.UtcNow.AddHours(1);
            var overpassEnd = overpassStart.AddMinutes(10);

            // The Command executes at 10:05 (During upload -> Impossible/Time Travel)
            var commandExecutionTime = overpassStart.AddMinutes(5);

            var flightPlan = new FlightPlanEntity
            {
                Id = planId,
                Status = FlightPlanStatus.Approved,
                SatelliteId = satId,
                GroundStationId = gsId
            };

            var cmd = new TriggerCaptureCommand
            {
                CaptureLocation = new CaptureLocation { Latitude = 0, Longitude = 0 },
                CameraSettings = new CameraSettings { CameraId = "Cam1", Type = CameraType.VMB, NumImages = 1, Iso = 1, IntervalMicroseconds = 1, ObservationId = 1, PipelineId = 1 }
            };
            flightPlan.SetCommands([cmd]);

            // Mocks
            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(flightPlan);
            _mockSatelliteService.Setup(s => s.GetAsync(satId)).ReturnsAsync(new SatelliteEntity { Id = satId, TleLine1 = validTle1, TleLine2 = validTle2 });

            _mockOverpassService.Setup(s => s.CalculateOverpassesAsync(It.IsAny<OverpassWindowsCalculationRequestDto>()))
                .ReturnsAsync([
                    new OverpassWindowDto {
                        StartTime = overpassStart,
                        EndTime = overpassEnd,
                        MaxElevationTime = overpassStart.AddMinutes(5)
                    }
                ]);

            _mockImagingCalculation.Setup(c => c.FindBestImagingOpportunity(It.IsAny<SGPdotNET.Observation.Satellite>(), It.IsAny<GeodeticCoordinate>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                .Returns(new ImagingCalculation.ImagingOpportunity { ImagingTime = commandExecutionTime });

            var dto = new AssignOverpassDto { StartTime = overpassStart, EndTime = overpassEnd };

            // Act
            var (success, message) = await _sut.AssignOverpassAsync(planId, dto);

            // Assert
            success.Should().BeFalse();
            message.Should().Contain("Chronology Error");
            message.Should().Contain("occurs before or during the upload overpass");
        }

        [Fact]
        public async Task AssignOverpassAsync_WhenConflictingPlanExists_ReturnsConflictError()
        {
            // Arrange (Conflict Scenario)
            var planId = 1;
            var existingPlanId = 2;
            var satId = 100;

            var validTle1 = "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992";
            var validTle2 = "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673";

            // FIX: Ensure start time is definitively in the future to pass "past time" validation
            var baseTime = DateTime.UtcNow.AddHours(2);
            var executionTime = baseTime.AddHours(1);

            var flightPlan = new FlightPlanEntity
            {
                Id = planId,
                Status = FlightPlanStatus.Approved,
                SatelliteId = satId,
                Name = "New Plan"
            };

            var cmd1 = new TriggerPipelineCommand { Mode = 1, ExecutionTime = executionTime };
            flightPlan.SetCommands([cmd1]);

            var existingPlan = new FlightPlanEntity { Id = existingPlanId, Status = FlightPlanStatus.AssignedToOverpass, Name = "Existing Plan" };
            var cmd2 = new TriggerPipelineCommand { Mode = 2, ExecutionTime = executionTime };
            existingPlan.SetCommands([cmd2]);

            // Mocks
            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(flightPlan);
            _mockSatelliteService.Setup(s => s.GetAsync(satId)).ReturnsAsync(new SatelliteEntity { Id = satId, TleLine1 = validTle1, TleLine2 = validTle2 });

            _mockOverpassService.Setup(s => s.CalculateOverpassesAsync(It.IsAny<OverpassWindowsCalculationRequestDto>()))
                .ReturnsAsync([
                    new OverpassWindowDto { StartTime = baseTime, EndTime = baseTime.AddMinutes(10), MaxElevationTime = baseTime.AddMinutes(5) }
                ]);

            _mockFlightPlanRepo.Setup(r => r.GetActivePlansBySatelliteAsync(satId))
                .ReturnsAsync([existingPlan]);

            var dto = new AssignOverpassDto { StartTime = baseTime, EndTime = baseTime.AddMinutes(10) };

            // Act
            var (success, message) = await _sut.AssignOverpassAsync(planId, dto);

            // Assert
            success.Should().BeFalse();
            message.Should().Contain("Conflict Error");
            message.Should().Contain("Existing Plan");
        }

        [Fact]
        public async Task AssignOverpassAsync_WhenValid_Success()
        {
            // Arrange
            var planId = 1;
            var satId = 100;

            // FIX: Use valid TLE strings
            var validTle1 = "1 25544U 98067A   23256.90616898  .00020137  00000-0  35438-3 0  9992";
            var validTle2 = "2 25544  51.6416 339.0970 0003835  48.3825  73.2709 15.50030022414673";

            var overpassStart = DateTime.UtcNow.AddHours(1);
            var overpassEnd = overpassStart.AddMinutes(10);
            var executionTime = overpassEnd.AddMinutes(30);

            var flightPlan = new FlightPlanEntity { Id = planId, Status = FlightPlanStatus.Approved, SatelliteId = satId, GroundStationId = 1 };
            flightPlan.SetCommands([new TriggerPipelineCommand { Mode = 1, ExecutionTime = executionTime }]);

            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(flightPlan);
            _mockSatelliteService.Setup(s => s.GetAsync(satId)).ReturnsAsync(new SatelliteEntity { Id = satId, TleLine1 = validTle1, TleLine2 = validTle2 });

            _mockOverpassService.Setup(s => s.CalculateOverpassesAsync(It.IsAny<OverpassWindowsCalculationRequestDto>()))
                .ReturnsAsync([
                    new OverpassWindowDto { StartTime = overpassStart, EndTime = overpassEnd, MaxElevationTime = overpassStart.AddMinutes(5) }
                ]);

            _mockFlightPlanRepo.Setup(r => r.GetActivePlansBySatelliteAsync(satId)).ReturnsAsync([]);

            _mockOverpassService.Setup(s => s.FindOrCreateOverpassForFlightPlanAsync(It.IsAny<OverpassWindowDto>(), planId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ReturnsAsync((true, new Entity { Id = 55 }, "Success"));

            var dto = new AssignOverpassDto { StartTime = overpassStart, EndTime = overpassEnd };

            // Act
            var (success, message) = await _sut.AssignOverpassAsync(planId, dto);

            // Assert
            success.Should().BeTrue();
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.Is<FlightPlanEntity>(fp => fp.Status == FlightPlanStatus.AssignedToOverpass)), Times.Once);
        }
    }
}