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


namespace SatOps.Tests
{
    public class FlightPlanServiceTests
    {
        // --- Mocks for all dependencies ---
        private readonly Mock<IFlightPlanRepository> _mockFlightPlanRepo;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IGroundStationService> _mockGroundStationService;
        private readonly Mock<ICurrentUserProvider> _mockCurrentUserProvider;

        private readonly FlightPlanService _sut;

        public FlightPlanServiceTests()
        {
            // Initialize all the mocks
            _mockFlightPlanRepo = new Mock<IFlightPlanRepository>();
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGroundStationService = new Mock<IGroundStationService>();
            var mockOverpassService = new Mock<IOverpassService>();
            var mockImagingCalculation = new Mock<IImagingCalculation>();
            _mockCurrentUserProvider = new Mock<ICurrentUserProvider>();

            // Create the service instance
            _sut = new FlightPlanService(
                _mockFlightPlanRepo.Object,
                _mockSatelliteService.Object,
                _mockGroundStationService.Object,
                mockOverpassService.Object,
                mockImagingCalculation.Object,
                _mockCurrentUserProvider.Object
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
                Commands = [] // Empty command list
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
            draftPlan.SetCommands([]); // Ensure commands are valid

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
    }
}