using System.Net;

namespace SatOps.Services.GroundStation
{
    public interface IGroundStationHealthService
    {
        Task<bool> CheckHealthAsync(GroundStation groundStation);
        Task<Dictionary<int, bool>> CheckAllStationsHealthAsync();
        Task UpdateStationHealthAsync(int stationId, bool isHealthy);
    }

    public class GroundStationHealthService : IGroundStationHealthService
    {
        private readonly HttpClient _httpClient;
        private readonly IGroundStationRepository _repository;
        private readonly ILogger<GroundStationHealthService> _logger;

        public GroundStationHealthService(
            HttpClient httpClient,
            IGroundStationRepository repository,
            ILogger<GroundStationHealthService> logger)
        {
            _httpClient = httpClient;
            _repository = repository;
            _logger = logger;

            // Configure HttpClient with reasonable timeouts for health checks
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<bool> CheckHealthAsync(GroundStation groundStation)
        {
            try
            {
                _logger.LogDebug("Checking health for ground station {Name} at {Url}",
                    groundStation.Name, groundStation.HttpUrl);

                // For now, we'll do a simple HTTP GET to the base URL
                // In a real scenario, you might have a dedicated health endpoint like /health
                var healthCheckUrl = $"{groundStation.HttpUrl.TrimEnd('/')}/health";

                var response = await _httpClient.GetAsync(healthCheckUrl);

                var isHealthy = response.StatusCode == HttpStatusCode.OK;

                _logger.LogInformation("Health check for ground station {Name}: {Status}",
                    groundStation.Name, isHealthy ? "Healthy" : "Unhealthy");

                return isHealthy;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error during health check for ground station {Name}",
                    groundStation.Name);
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("Timeout during health check for ground station {Name}",
                    groundStation.Name);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during health check for ground station {Name}",
                    groundStation.Name);
                return false;
            }
        }

        public async Task<Dictionary<int, bool>> CheckAllStationsHealthAsync()
        {
            var stations = await _repository.GetAllAsync();
            var healthResults = new Dictionary<int, bool>();

            var healthCheckTasks = stations.Select(async station =>
            {
                var isHealthy = await CheckHealthAsync(station);
                return new { StationId = station.Id, IsHealthy = isHealthy };
            });

            var results = await Task.WhenAll(healthCheckTasks);

            foreach (var result in results)
            {
                healthResults[result.StationId] = result.IsHealthy;
            }

            return healthResults;
        }

        public async Task UpdateStationHealthAsync(int stationId, bool isHealthy)
        {
            var station = await _repository.GetByIdAsync(stationId);
            if (station == null)
            {
                _logger.LogWarning("Attempted to update health for non-existent ground station {StationId}",
                    stationId);
                return;
            }

            if (station.IsActive != isHealthy)
            {
                station.IsActive = isHealthy;
                station.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(station);

                _logger.LogInformation("Updated health status for ground station {Name} to {Status}",
                    station.Name, isHealthy ? "Active" : "Inactive");
            }
        }
    }
}
