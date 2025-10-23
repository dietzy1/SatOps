namespace SatOps.Modules.Groundstation
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
            _logger.LogDebug("Ground Station Health Check Worker started with interval: {Interval}", _checkInterval);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformHealthChecksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error occurred during health check cycle");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Ground Station Health Check Worker stopped");
        }

        private async Task PerformHealthChecksAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var gatewayService = scope.ServiceProvider.GetRequiredService<Gateway.IGroundStationGatewayService>();
            var repository = scope.ServiceProvider.GetRequiredService<IGroundStationRepository>();

            _logger.LogDebug("Starting health check cycle");

            var stations = await repository.GetAllAsync();
            var disconnectedStations = stations.Where(s => !gatewayService.IsGroundStationConnected(s.Id)).ToList();

            if (disconnectedStations.Count > 0)
            {
                foreach (var station in disconnectedStations)
                {
                    _logger.LogWarning("Ground station {StationId} ({Name}) is not connected via WebSocket",
                        station.Id, station.Name);
                }
            }

            _logger.LogDebug("Completed health check cycle for {TotalCount} stations ({ConnectedCount} connected, {DisconnectedCount} disconnected)",
                stations.Count,
                stations.Count - disconnectedStations.Count,
                disconnectedStations.Count);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ground Station Health Check Worker is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
