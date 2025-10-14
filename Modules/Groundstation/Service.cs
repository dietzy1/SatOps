using SatOps.Modules.Groundstation.Health;
using SatOps.Utils;
using System.Security.Cryptography;

// Other stuff we must figure out how to support.
// Groundstations will send data to us, imageformat, logs etc we must have some sort of reciever endpoints for that that isn't exposed to the public
// We must also have some sender endpoints for us to send commands to the groundstations
// - Sender service for sending commands to groundstations
// - Reciever service for receiving data from groundstations

// What other stuff is needed for groundstations?
// - Telemetry data processing
// - Command scheduling
// - Health monitoring
// - Configuration management

namespace SatOps.Modules.Groundstation
{
    public interface IGroundStationService
    {
        Task<List<GroundStation>> ListAsync();
        Task<GroundStation?> GetAsync(int id);
        Task<(GroundStation createdStation, string rawApiKey)> CreateAsync(GroundStation entity);
        Task<GroundStation?> PatchAsync(int id, GroundStationPatchDto patchDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateHealthStatusAsync(int id, bool isActive);
        Task<(GroundStation? station, bool isHealthy)> GetRealTimeHealthStatusAsync(int id);
        Task<List<GroundStation>> GetActiveStationsAsync();
    }

    public class GroundStationService(IGroundStationRepository repository, IGroundStationHealthService healthService) : IGroundStationService
    {
        private const double Epsilon = 1e-6;
        public Task<List<GroundStation>> ListAsync() => repository.GetAllAsync();

        public Task<GroundStation?> GetAsync(int id) => repository.GetByIdAsync(id);

        public async Task<(GroundStation createdStation, string rawApiKey)> CreateAsync(GroundStation entity)
        {
            // Generate a URL-safe API key
            var bytes = RandomNumberGenerator.GetBytes(32);
            var rawApiKey = Convert.ToBase64String(bytes)
                                .Replace('+', '-')
                                .Replace('/', '_');

            entity.ApiKeyHash = ApiKeyHasher.Hash(rawApiKey);
            entity.ApplicationId = Guid.NewGuid();

            var createdEntity = await repository.AddAsync(entity);

            return (createdEntity, rawApiKey);
        }

        public async Task<GroundStation?> PatchAsync(int id, GroundStationPatchDto patchDto)
        {
            var existing = await repository.GetByIdTrackedAsync(id);
            if (existing == null)
            {
                return null;
            }

            bool hasChanges = false;

            if (patchDto.Name != null && existing.Name != patchDto.Name)
            {
                existing.Name = patchDto.Name;
                hasChanges = true;
            }

            if (patchDto.Location != null)
            {
                // Only update if there are actual changes
                var loc = existing.Location;
                var newLat = patchDto.Location.Latitude ?? loc.Latitude;
                var newLon = patchDto.Location.Longitude ?? loc.Longitude;
                var newAlt = patchDto.Location.Altitude ?? loc.Altitude;

                if (IsDifferent(loc.Latitude, newLat) ||
                    IsDifferent(loc.Longitude, newLon) ||
                    IsDifferent(loc.Altitude, newAlt))
                {
                    existing.Location = new Location { Latitude = newLat, Longitude = newLon, Altitude = newAlt };
                    hasChanges = true;
                }
            }

            if (patchDto.HttpUrl != null && existing.HttpUrl != patchDto.HttpUrl)
            {
                existing.HttpUrl = patchDto.HttpUrl;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return existing;
            }

            return await repository.UpdateAsync(existing);
        }

        public Task<bool> DeleteAsync(int id) => repository.DeleteAsync(id);

        public async Task<List<GroundStation>> GetActiveStationsAsync()
        {
            var allStations = await repository.GetAllAsync();
            return allStations.Where(s => s.IsActive).ToList();
        }

        public async Task<bool> UpdateHealthStatusAsync(int id, bool isActive)
        {
            var existing = await repository.GetByIdTrackedAsync(id);
            if (existing == null) return false;

            if (existing.IsActive == isActive)
            {
                return true;
            }

            existing.IsActive = isActive;

            var updated = await repository.UpdateAsync(existing);
            return updated != null;
        }

        public async Task<(GroundStation? station, bool isHealthy)> GetRealTimeHealthStatusAsync(int id)
        {
            var station = await repository.GetByIdAsync(id);
            if (station == null) return (null, false);

            // Make actual HTTP call to the ground station's health endpoint
            var isHealthy = await healthService.CheckHealthAsync(station);

            // Optionally update the database with the current health status
            if (station.IsActive != isHealthy)
            {
                await healthService.UpdateStationHealthAsync(id, isHealthy);
            }

            return (station, isHealthy);
        }

        private static bool IsDifferent(double a, double b) => Math.Abs(a - b) > Epsilon;
    }
}

