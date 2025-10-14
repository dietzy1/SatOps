namespace SatOps.Modules.Groundstation.Health
{
    public class GroundStationHealthCheckWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GroundStationHealthCheckWorker> _logger;
        private readonly TimeSpan _checkInterval;

        public GroundStationHealthCheckWorker(
            IServiceProvider serviceProvider,
            ILogger<GroundStationHealthCheckWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var intervalSeconds = configuration.GetValue("GroundStationHealthCheck:IntervalSeconds", 120);
            _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ground Station Health Check Worker started with interval: {Interval}", _checkInterval);

            // Wait a bit before starting the first check to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformHealthChecksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during health check cycle");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
            }

            _logger.LogInformation("Ground Station Health Check Worker stopped");
        }

        private async Task PerformHealthChecksAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var healthService = scope.ServiceProvider.GetRequiredService<IGroundStationHealthService>();

            _logger.LogDebug("Starting health check cycle");

            var healthResults = await healthService.CheckAllStationsHealthAsync();

            foreach (var (stationId, isHealthy) in healthResults)
            {
                await healthService.UpdateStationHealthAsync(stationId, isHealthy);
            }

            _logger.LogDebug("Completed health check cycle for {Count} stations", healthResults.Count);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ground Station Health Check Worker is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
