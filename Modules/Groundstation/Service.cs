using System.Collections.Generic;
using System.Threading.Tasks;
using SatOps.Modules.Groundstation.Health;

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
        Task<GroundStation> CreateAsync(GroundStation entity);
        Task<GroundStation?> UpdateAsync(int id, GroundStation entity);
        Task<GroundStation?> PatchAsync(int id, GroundStation partial);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateHealthStatusAsync(int id, bool isActive);
        Task<(GroundStation? station, bool isHealthy)> GetRealTimeHealthStatusAsync(int id);
    }

    public class GroundStationService : IGroundStationService
    {
        private readonly IGroundStationRepository _repository;
        private readonly IGroundStationHealthService _healthService;

        public GroundStationService(IGroundStationRepository repository, IGroundStationHealthService healthService)
        {
            _repository = repository;
            _healthService = healthService;
        }

        public Task<List<GroundStation>> ListAsync() => _repository.GetAllAsync();

        public Task<GroundStation?> GetAsync(int id) => _repository.GetByIdAsync(id);

        public Task<GroundStation> CreateAsync(GroundStation entity) => _repository.AddAsync(entity);

        public async Task<GroundStation?> UpdateAsync(int id, GroundStation entity)
        {
            entity.Id = id;
            return await _repository.UpdateAsync(entity);
        }

        public async Task<GroundStation?> PatchAsync(int id, GroundStation partial)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            if (!string.IsNullOrWhiteSpace(partial.Name)) existing.Name = partial.Name;
            if (partial.Location != null) existing.Location = partial.Location;
            if (!string.IsNullOrWhiteSpace(partial.HttpUrl)) existing.HttpUrl = partial.HttpUrl;
            existing.IsActive = partial.IsActive != existing.IsActive ? partial.IsActive : existing.IsActive;

            return await _repository.UpdateAsync(existing);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

        public async Task<List<GroundStation>> GetActiveStationsAsync()
        {
            var allStations = await _repository.GetAllAsync();
            return allStations.Where(s => s.IsActive).ToList();
        }

        public async Task<bool> UpdateHealthStatusAsync(int id, bool isActive)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.IsActive = isActive;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing);
            return updated != null;
        }

        public async Task<(GroundStation? station, bool isHealthy)> GetRealTimeHealthStatusAsync(int id)
        {
            var station = await _repository.GetByIdAsync(id);
            if (station == null) return (null, false);

            // Make actual HTTP call to the ground station's health endpoint
            var isHealthy = await _healthService.CheckHealthAsync(station);

            // Optionally update the database with the current health status
            if (station.IsActive != isHealthy)
            {
                await _healthService.UpdateStationHealthAsync(id, isHealthy);
            }

            return (station, isHealthy);
        }
    }
}

