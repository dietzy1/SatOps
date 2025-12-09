using Moq;
using Xunit;
using FluentAssertions;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.User;
using SatOps.Modules.Satellite;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using SatOps.Modules.FlightPlan.Commands;
using Microsoft.Extensions.Options;
using SatOps.Configuration;
using Microsoft.Extensions.Logging;

namespace SatOps.Tests.FlightPlan
{
    public class FlightPlanServiceValidationTests
    {
        private readonly Mock<IFlightPlanRepository> _mockFlightPlanRepo;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IGroundStationService> _mockGroundStationService;
        private readonly Mock<ICurrentUserProvider> _mockCurrentUserProvider;
        private readonly FlightPlanService _sut;

        public FlightPlanServiceValidationTests()
        {
            _mockFlightPlanRepo = new Mock<IFlightPlanRepository>();
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGroundStationService = new Mock<IGroundStationService>();
            var mockOverpassService = new Mock<IOverpassService>();
            var mockImagingCalculation = new Mock<IImagingCalculation>();
            _mockCurrentUserProvider = new Mock<ICurrentUserProvider>();
            var mockLogger = new Mock<ILogger<FlightPlanService>>();

            var mockImagingOptions = new Mock<IOptions<ImagingCalculationOptions>>();
            mockImagingOptions.Setup(o => o.Value).Returns(new ImagingCalculationOptions());

            _sut = new FlightPlanService(
                _mockFlightPlanRepo.Object,
                _mockSatelliteService.Object,
                _mockGroundStationService.Object,
                mockOverpassService.Object,
                mockImagingCalculation.Object,
                _mockCurrentUserProvider.Object,
                mockImagingOptions.Object,
                mockLogger.Object
            );

            _mockCurrentUserProvider.Setup(p => p.GetUserId()).Returns(1);
            _mockSatelliteService.Setup(s => s.GetAsync(It.IsAny<int>())).ReturnsAsync(new SatelliteEntity());
            _mockGroundStationService.Setup(s => s.GetAsync(It.IsAny<int>())).ReturnsAsync(new GroundStation());
        }

        private CreateFlightPlanDto CreateValidDto()
        {
            return new CreateFlightPlanDto
            {
                Name = "Valid Plan",
                GsId = 1,
                SatId = 1,
                Commands =
                [
                    new TriggerPipelineCommand { Mode = 1, ExecutionTime = DateTime.UtcNow }
                ]
            };
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentSatelliteId_ThrowsArgumentException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.SatId = 999;
            _mockSatelliteService.Setup(s => s.GetAsync(dto.SatId)).ReturnsAsync((SatelliteEntity?)null);

            // Act
            Func<Task> act = () => _sut.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Satellite with ID {dto.SatId} not found. (Parameter 'SatId')");
        }

        [Fact]
        public async Task CreateAsync_WithNonExistentGroundStationId_ThrowsArgumentException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.GsId = 888;
            _mockGroundStationService.Setup(s => s.GetAsync(dto.GsId)).ReturnsAsync((GroundStation?)null);

            // Act
            Func<Task> act = () => _sut.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Ground station with ID {dto.GsId} not found. (Parameter 'GsId')");
        }

        [Fact]
        public async Task CreateAsync_WithInvalidCommand_ThrowsArgumentException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.Commands =
            [
                new TriggerPipelineCommand { Mode = 1, ExecutionTime = null } // Invalid command
            ];

            // Act
            Func<Task> act = () => _sut.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Command validation failed: Command 1 (TRIGGER_PIPELINE): ExecutionTime is required");
        }
    }
}