using System.Collections.Generic;
using System.Threading.Tasks;

namespace SatOps.Services.GroundStation
{
    public interface IGroundStationService
    {
        Task<List<GroundStation>> ListAsync();
        Task<GroundStation?> GetAsync(int id);
        Task<GroundStation> CreateAsync(GroundStation entity);
        Task<GroundStation?> UpdateAsync(int id, GroundStation entity);
        Task<GroundStation?> PatchAsync(int id, GroundStation partial);
        Task<bool> DeleteAsync(int id);
    }

    public class GroundStationService : IGroundStationService
    {
        private readonly IGroundStationRepository _repository;

        public GroundStationService(IGroundStationRepository repository)
        {
            _repository = repository;
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
    }
}

