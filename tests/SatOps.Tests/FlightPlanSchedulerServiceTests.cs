using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.GroundStationLink;
using SatOps.Modules.Satellite;
using System.Reflection;

namespace SatOps.Tests
{
    // A testable version of the scheduler that allows us to call ExecuteAsync
    public class TestableSchedulerService(IServiceProvider serviceProvider, ILogger<SchedulerService> logger) : SchedulerService(serviceProvider, logger)
    {
        public Task InvokeExecuteAsync(CancellationToken token)
        {
            // Use reflection to call the protected ExecuteAsync method
            return (Task)GetType().BaseType!
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(this, [token])!;
        }
    }

    public class SchedulerServiceTests
    {
        private readonly Mock<IFlightPlanService> _mockFlightPlanService;
        private readonly Mock<ISatelliteService> _mockSatelliteService;
        private readonly Mock<IWebSocketService> _mockGatewayService;
        private readonly IServiceProvider _serviceProvider;

        public SchedulerServiceTests()
        {
            _mockFlightPlanService = new Mock<IFlightPlanService>();
            _mockSatelliteService = new Mock<ISatelliteService>();
            _mockGatewayService = new Mock<IWebSocketService>();

            // Create a mock IServiceProvider that returns our mocked services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped(_ => _mockFlightPlanService.Object);
            serviceCollection.AddScoped(_ => _mockSatelliteService.Object);
            serviceCollection.AddScoped(_ => _mockGatewayService.Object);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public async Task Scheduler_WhenPlanIsReadyAndGSIsConnected_TransmitsAndUpdateStatus()
        {
            // Arrange
            var planId = 1;
            var gsId = 10;
            var satId = 20;
            var scheduledTime = DateTime.UtcNow.AddMinutes(2);
            var plan = new FlightPlan { Id = planId, GroundStationId = gsId, SatelliteId = satId, ScheduledAt = scheduledTime };
            var satellite = new Satellite { Id = satId, Name = "SAT-1" };
            var script = new List<string> { "do_something" };
            var cts = new CancellationTokenSource();

            // Setup mocks
            _mockFlightPlanService.Setup(s => s.GetPlansReadyForTransmissionAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync([plan]); // Return one plan to process
            _mockGatewayService.Setup(g => g.IsGroundStationConnected(gsId)).Returns(true);
            _mockSatelliteService.Setup(s => s.GetAsync(satId)).ReturnsAsync(satellite);
            _mockFlightPlanService.Setup(s => s.CompileFlightPlanToCshAsync(planId)).ReturnsAsync(script);

            var loggerMock = new Mock<ILogger<SchedulerService>>();
            var scheduler = new TestableSchedulerService(_serviceProvider, loggerMock.Object);

            // Act
            // Run the scheduler loop just once
            cts.CancelAfter(TimeSpan.FromSeconds(1)); // Stop the loop after the first run
            try
            {
                await scheduler.InvokeExecuteAsync(cts.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }

            // Assert
            // Verify the command was sent via the gateway
            _mockGatewayService.Verify(g => g.SendScheduledCommand(gsId, satId, satellite.Name, planId, scheduledTime, script), Times.Once);

            // Verify the plan's status was updated to Transmitted
            _mockFlightPlanService.Verify(s => s.UpdateFlightPlanStatusAsync(planId, FlightPlanStatus.Transmitted, null), Times.Once);
        }

        [Fact]
        public async Task Scheduler_WhenGSIsOffline_MarksPlanAsFailedIfImminent()
        {
            // Arrange
            var planId = 1;
            var gsId = 10;
            var scheduledTime = DateTime.UtcNow.AddSeconds(30); // Imminent!
            var plan = new FlightPlan { Id = planId, GroundStationId = gsId, ScheduledAt = scheduledTime };
            var cts = new CancellationTokenSource();

            _mockFlightPlanService.Setup(s => s.GetPlansReadyForTransmissionAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync([plan]);
            _mockGatewayService.Setup(g => g.IsGroundStationConnected(gsId)).Returns(false); // GS is offline

            var loggerMock = new Mock<ILogger<SchedulerService>>();
            var scheduler = new TestableSchedulerService(_serviceProvider, loggerMock.Object);

            // Act
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            try
            {
                await scheduler.InvokeExecuteAsync(cts.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }

            // Assert
            // Verify NO command was sent
            _mockGatewayService.Verify(g => g.SendScheduledCommand(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<string>>()), Times.Never);

            // Verify the plan was marked as Failed with a reason
            _mockFlightPlanService.Verify(s => s.UpdateFlightPlanStatusAsync(
                planId,
                FlightPlanStatus.Failed,
                $"Ground Station {gsId} is not connected."
            ), Times.Once);
        }

        [Fact]
        public async Task Scheduler_WhenNoPlansAreReady_DoesNothing()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            _mockFlightPlanService.Setup(s => s.GetPlansReadyForTransmissionAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync([]); // No plans returned

            var loggerMock = new Mock<ILogger<SchedulerService>>();
            var scheduler = new TestableSchedulerService(_serviceProvider, loggerMock.Object);

            // Act
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            try
            {
                await scheduler.InvokeExecuteAsync(cts.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }

            // Assert
            _mockGatewayService.Verify(g => g.SendScheduledCommand(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<string>>()), Times.Never);
            _mockFlightPlanService.Verify(s => s.UpdateFlightPlanStatusAsync(It.IsAny<int>(), It.IsAny<FlightPlanStatus>(), It.IsAny<string>()), Times.Never);
        }
    }
}