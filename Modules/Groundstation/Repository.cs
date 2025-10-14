
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

    public class GroundStationRepository(SatOpsDbContext dbContext) : IGroundStationRepository
    {
        public Task<List<GroundStation>> GetAllAsync()
        {
            return dbContext.GroundStations.AsNoTracking().ToListAsync();
        }

        public Task<GroundStation?> GetByIdAsync(int id)
        {
            return dbContext.GroundStations.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        }
        public Task<GroundStation?> GetByIdTrackedAsync(int id)
        {
            return dbContext.GroundStations.FirstOrDefaultAsync(g => g.Id == id);
        }

        public Task<GroundStation?> GetByApplicationIdAsync(Guid applicationId)
        {
            return dbContext.GroundStations
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.ApplicationId == applicationId);
        }

        public async Task<GroundStation> AddAsync(GroundStation entity)
        {
            dbContext.GroundStations.Add(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<GroundStation?> UpdateAsync(GroundStation entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await dbContext.GroundStations.FirstOrDefaultAsync(g => g.Id == id);
            if (existing == null)
            {
                return false;
            }
            dbContext.GroundStations.Remove(existing);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}

