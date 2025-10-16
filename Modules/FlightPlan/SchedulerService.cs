using SatOps.Modules.Gateway;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.FlightPlan
{
    public class SchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SchedulerService> _logger;

        public SchedulerService(IServiceProvider serviceProvider, ILogger<SchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flight Plan Scheduler Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scheduler running job check at: {time}", DateTimeOffset.Now);

                // We create a new scope for each run
                using (var scope = _serviceProvider.CreateScope())
                {
                    var flightPlanService = scope.ServiceProvider.GetRequiredService<IFlightPlanService>();
                    var satelliteService = scope.ServiceProvider.GetRequiredService<ISatelliteService>();
                    var gatewayService = scope.ServiceProvider.GetRequiredService<IGroundStationGatewayService>();

                    // Look ahead 5 minutes to find flight plans to transmit.
                    var plansToSend = await flightPlanService.GetPlansReadyForTransmissionAsync(TimeSpan.FromMinutes(5));

                    foreach (var plan in plansToSend)
                    {
                        if (gatewayService.IsGroundStationConnected(plan.GroundStationId))
                        {
                            try
                            {
                                _logger.LogInformation("Found plan {PlanId} for GS {GSId}, scheduled for {ScheduledTime}. Preparing for transmission.", plan.Id, plan.GroundStationId, plan.ScheduledAt);

                                var satellite = await satelliteService.GetAsync(plan.SatelliteId);
                                if (satellite == null)
                                {
                                    {
                                        var reason = $"Satellite with ID {plan.SatelliteId} not found in database.";
                                        _logger.LogError("{Reason} for flight plan {PlanId}. Skipping.", reason, plan.Id);
                                        await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Failed, reason);
                                        continue;
                                    }
                                }

                                var cshScript = await flightPlanService.CompileFlightPlanToCshAsync(plan.Id);

                                await gatewayService.SendScheduledCommand(
                                    plan.GroundStationId,
                                    satellite.Name,
                                    plan.ScheduledAt.GetValueOrDefault(),
                                    cshScript
                                );

                                // Update status to prevent re-sending.
                                await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Transmitted);
                                _logger.LogInformation("Successfully transmitted flight plan {PlanId} to Ground Station {GSId}.", plan.Id, plan.GroundStationId);
                            }
                            catch (Exception ex)
                            {
                                var reason = $"An exception occurred during transmission: {ex.Message}";
                                _logger.LogError(ex, "Failed to transmit flight plan {PlanId} to GS {GSId}", plan.Id, plan.GroundStationId);
                                await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Failed, reason);
                            }
                        }
                        else
                        {
                            var reason = $"Ground Station {plan.GroundStationId} is not connected.";
                            _logger.LogWarning("Cannot transmit flight plan {PlanId}: {Reason}", plan.Id, reason);

                            if (plan.ScheduledAt.HasValue && (plan.ScheduledAt.Value - DateTime.UtcNow) < TimeSpan.FromMinutes(1))
                            {
                                _logger.LogError("Marking imminent flight plan {PlanId} as FAILED because GS is offline.", plan.Id);
                                await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Failed, reason);
                            }
                        }
                    }
                }

                // Wait for 30 seconds before the next check.
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}