using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Satellite
{
    public interface ISatelliteRepository
    {
        Task<List<Satellite>> GetAllAsync();
        Task<Satellite?> GetByIdAsync(int id);
        Task UpdateTleAsync(int id, string tleLine1, string tleLine2);
        Task UpdateLastUpdateTimestampAsync(int id);

    }

    public class SatelliteRepository(SatOpsDbContext dbContext) : ISatelliteRepository
    {
        public Task<List<Satellite>> GetAllAsync()
        {
            return dbContext.Satellites.AsNoTracking().ToListAsync();
        }

        public Task<Satellite?> GetByIdAsync(int id)
        {
            return dbContext.Satellites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateTleAsync(int id, string tleLine1, string tleLine2)
        {
            var existing = await dbContext.Satellites.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return;

            existing.TleLine1 = tleLine1;
            existing.TleLine2 = tleLine2;
            existing.LastUpdate = DateTime.UtcNow;

            dbContext.Satellites.Update(existing);
            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateLastUpdateTimestampAsync(int id)
        {
            var existing = await dbContext.Satellites.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return;

            existing.LastUpdate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}
