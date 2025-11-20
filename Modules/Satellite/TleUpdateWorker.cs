namespace SatOps.Modules.Satellite
{
    public class TleUpdateWorker(
        IServiceProvider serviceProvider,
        ILogger<TleUpdateWorker> logger) : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("TLE Update Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateAllSatellitesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Critical failure in TLE Update Worker cycle");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task UpdateAllSatellitesAsync(CancellationToken ct)
        {
            using var scope = serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISatelliteService>();
            var repository = scope.ServiceProvider.GetRequiredService<ISatelliteRepository>();

            var satellites = await repository.GetAllAsync();
            var activeSatellites = satellites.Where(s => s.Status == SatelliteStatus.Active);

            foreach (var sat in activeSatellites)
            {
                if (ct.IsCancellationRequested) break;

                logger.LogInformation("Updating TLE for {SatelliteName}...", sat.Name);

                var success = await service.RefreshTleDataAsync(sat.Id);

                if (success == true)
                    logger.LogInformation("TLE updated successfully for {SatelliteName}", sat.Name);
                else
                    logger.LogWarning("Failed to fetch new TLE for {SatelliteName}. Using cached data.", sat.Name);

                await Task.Delay(2000, ct);
            }
        }
    }
}