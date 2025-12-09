using SatOps.Modules.GroundStationLink;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.FlightPlan
{
    public class SchedulerService(IServiceProvider serviceProvider, ILogger<SchedulerService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Flight Plan Scheduler Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Scheduler running job check at: {time}", DateTimeOffset.Now);

                // We create a new scope for each run
                using (var scope = serviceProvider.CreateScope())
                {
                    var flightPlanService = scope.ServiceProvider.GetRequiredService<IFlightPlanService>();
                    var satelliteService = scope.ServiceProvider.GetRequiredService<ISatelliteService>();
                    var gatewayService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();

                    // Look ahead 5 minutes to find flight plans to transmit.
                    var plansToSend = await flightPlanService.GetPlansReadyForTransmissionAsync(TimeSpan.FromMinutes(5));

                    foreach (var plan in plansToSend)
                    {
                        // Skip plans that don't have a ground station assigned
                        if (!plan.GroundStationId.HasValue)
                        {
                            logger.LogWarning("Flight plan {PlanId} has no assigned ground station. Skipping.", plan.Id);
                            continue;
                        }

                        if (gatewayService.IsGroundStationConnected(plan.GroundStationId.Value))
                        {
                            try
                            {
                                logger.LogInformation("Found plan {PlanId} for GS {GSId}, scheduled for {ScheduledTime}. Preparing for transmission.", plan.Id, plan.GroundStationId.Value, plan.ScheduledAt);

                                var satellite = await satelliteService.GetAsync(plan.SatelliteId);
                                if (satellite == null)
                                {
                                    {
                                        var reason = $"Satellite with ID {plan.SatelliteId} not found in database.";
                                        logger.LogError("{Reason} for flight plan {PlanId}. Skipping.", reason, plan.Id);
                                        await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Failed, reason);
                                        continue;
                                    }
                                }

                                var cshScript = await flightPlanService.CompileFlightPlanToCshAsync(plan.Id);

                                await gatewayService.SendScheduledCommand(
                                    plan.GroundStationId.Value,
                                    plan.SatelliteId,
                                    satellite.Name,
                                    plan.Id,
                                    plan.ScheduledAt.GetValueOrDefault(),
                                    cshScript
                                );

                                // Update status to prevent re-sending.
                                await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Transmitted);
                                logger.LogInformation("Successfully transmitted flight plan {PlanId} to Ground Station {GSId}.", plan.Id, plan.GroundStationId.Value);
                            }
                            catch (Exception ex)
                            {
                                var reason = $"An exception occurred during transmission: {ex.Message}";
                                logger.LogError(ex, "Failed to transmit flight plan {PlanId} to GS {GSId}", plan.Id, plan.GroundStationId.Value);
                                await flightPlanService.UpdateFlightPlanStatusAsync(plan.Id, FlightPlanStatus.Failed, reason);
                            }
                        }
                        else
                        {
                            var reason = $"Ground Station {plan.GroundStationId.Value} is not connected.";
                            logger.LogWarning("Cannot transmit flight plan {PlanId}: {Reason}", plan.Id, reason);

                            if (plan.ScheduledAt.HasValue && (plan.ScheduledAt.Value - DateTime.UtcNow) < TimeSpan.FromMinutes(1))
                            {
                                logger.LogError("Marking imminent flight plan {PlanId} as FAILED because GS is offline.", plan.Id);
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