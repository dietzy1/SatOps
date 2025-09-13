using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SatOps.Services.Satellite
{
    public interface ISatelliteService
    {
        Task<List<Satellite>> ListAsync();
        Task<Satellite?> GetAsync(int id);
        Task<bool> UpdateTleDataAsync(int id, string tleLine1, string tleLine2);
        Task<List<Satellite>> GetActiveSatellitesAsync();
    }

    public class SatelliteService : ISatelliteService
    {
        private readonly ISatelliteRepository _repository;

        public SatelliteService(ISatelliteRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Satellite>> ListAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<Satellite?> GetAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }



        public async Task<Satellite?> UpdateAsync(int id, Satellite entity)
        {
            entity.Id = id;
            return await _repository.UpdateAsync(entity);
        }
        public async Task<bool> UpdateTleDataAsync(int id, string tleLine1, string tleLine2)
        {
            var satellite = await _repository.GetByIdAsync(id);
            if (satellite == null) return false;

            satellite.TleLine1 = tleLine1;
            satellite.TleLine2 = tleLine2;
            satellite.LastTleUpdate = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(satellite);
            return updated != null;
        }

        public async Task<List<Satellite>> GetActiveSatellitesAsync()
        {
            var allSatellites = await _repository.GetAllAsync();
            return allSatellites.Where(s => s.Status == SatelliteStatus.Active).ToList();
        }
    }
}
