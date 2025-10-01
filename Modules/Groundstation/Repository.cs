
using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Groundstation
{
    public interface IGroundStationRepository
    {
        Task<List<GroundStation>> GetAllAsync();
        Task<GroundStation?> GetByIdAsync(int id);
        Task<GroundStation?> GetByIdTrackedAsync(int id);
        Task<GroundStation?> GetByApplicationIdAsync(Guid applicationId);
        Task<GroundStation> AddAsync(GroundStation entity);
        Task<GroundStation?> UpdateAsync(GroundStation entity);
        Task<bool> DeleteAsync(int id);
    }

    public class GroundStationRepository : IGroundStationRepository
    {
        private readonly SatOpsDbContext _dbContext;

        public GroundStationRepository(SatOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<GroundStation>> GetAllAsync()
        {
            return _dbContext.GroundStations.AsNoTracking().ToListAsync();
        }

        public Task<GroundStation?> GetByIdAsync(int id)
        {
            return _dbContext.GroundStations.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        }
        public Task<GroundStation?> GetByIdTrackedAsync(int id)
        {
            return _dbContext.GroundStations.FirstOrDefaultAsync(g => g.Id == id);
        }

        public Task<GroundStation?> GetByApplicationIdAsync(Guid applicationId)
        {
            return _dbContext.GroundStations
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.ApplicationId == applicationId);
        }

        public async Task<GroundStation> AddAsync(GroundStation entity)
        {
            _dbContext.GroundStations.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<GroundStation?> UpdateAsync(GroundStation entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.GroundStations.FirstOrDefaultAsync(g => g.Id == id);
            if (existing == null)
            {
                return false;
            }
            _dbContext.GroundStations.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

