using Moq;
using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.User;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using System.Text.Json;

namespace SatOps.Tests
{
    public class FlightPlanServiceTests
    {
        // --- Mocks for all dependencies ---
        private readonly Mock<IFlightPlanRepository> _mockFlightPlanRepo;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IGroundStationService> _mockGroundStationService;
        private readonly Mock<IOverpassService> _mockOverpassService;
        private readonly Mock<IImagingCalculation> _mockImagingCalculation;
        private readonly Mock<ICurrentUserProvider> _mockCurrentUserProvider;

        private readonly FlightPlanService _sut;

        public FlightPlanServiceTests()
        {
            // Initialize all the mocks
            _mockFlightPlanRepo = new Mock<IFlightPlanRepository>();
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGroundStationService = new Mock<IGroundStationService>();
            _mockOverpassService = new Mock<IOverpassService>();
            _mockImagingCalculation = new Mock<IImagingCalculation>();
            _mockCurrentUserProvider = new Mock<ICurrentUserProvider>();

            // Create the service instance
            _sut = new FlightPlanService(
                _mockFlightPlanRepo.Object,
                _mockSatelliteService.Object,
                _mockGroundStationService.Object,
                _mockOverpassService.Object,
                _mockImagingCalculation.Object,
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
                Commands = JsonDocument.Parse("[]").RootElement
            };

            // Setup mocks
            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(testUserId);
            _mockGroundStationService.Setup(s => s.GetAsync(createDto.GsId)).ReturnsAsync(new GroundStation());
            _mockSatelliteService.Setup(s => s.GetAsync(createDto.SatId)).ReturnsAsync(new Satellite());
            _mockFlightPlanRepo.Setup(r => r.AddAsync(It.IsAny<FlightPlan>()))
                .ReturnsAsync((FlightPlan fp) => fp); // Return the plan that was passed in

            // Act
            var result = await _sut.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.CreatedById.Should().Be(testUserId);

            // Verify that the repository was called with an entity that has the correct user ID
            _mockFlightPlanRepo.Verify(r => r.AddAsync(It.Is<FlightPlan>(fp => fp.CreatedById == testUserId)), Times.Once);
        }

        [Theory]
        [InlineData("APPROVED")]
        [InlineData("REJECTED")]
        public async Task ApproveOrRejectAsync_WhenPlanIsDraft_UpdatesStatusAndAssignsCurrentUserAsApprover(string targetStatus)
        {
            // Arrange
            var testUserId = 456;
            var planId = 1;
            var draftPlan = new FlightPlan { Id = planId, Status = FlightPlanStatus.Draft };

            // Setup mocks
            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(testUserId);
            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(draftPlan);

            // Act
            var (success, _) = await _sut.ApproveOrRejectAsync(planId, targetStatus);

            // Assert
            success.Should().BeTrue();

            // Verify that UpdateAsync was called with the correct status AND the correct user ID
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.Is<FlightPlan>(p =>
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
            var approvedPlan = new FlightPlan { Id = planId, Status = FlightPlanStatus.Approved };

            _mockFlightPlanRepo.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(approvedPlan);

            // We need to simulate an authenticated user, even if we don't use the ID.
            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(123);

            // Act
            var (success, message) = await _sut.ApproveOrRejectAsync(planId, "APPROVED");

            // Assert
            success.Should().BeFalse();
            message.Should().Be("Cannot modify a plan that has already been approved.");

            // Verify that we NEVER called the update method
            _mockFlightPlanRepo.Verify(r => r.UpdateAsync(It.IsAny<FlightPlan>()), Times.Never);
        }
    }
}