using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Satellite
{
    public interface ISatelliteRepository
    {
        Task<List<Satellite>> GetAllAsync();
        Task<Satellite?> GetByIdAsync(int id);
        Task UpdateTleAsync(int id, string tleLine1, string tleLine2);
    }

    public class SatelliteRepository : ISatelliteRepository
    {
        private readonly SatOpsDbContext _dbContext;

        public SatelliteRepository(SatOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<Satellite>> GetAllAsync()
        {
            return _dbContext.Satellites.AsNoTracking().ToListAsync();
        }

        public Task<Satellite?> GetByIdAsync(int id)
        {
            return _dbContext.Satellites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateTleAsync(int id, string tleLine1, string tleLine2)
        {
            var existing = await _dbContext.Satellites.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return;

            existing.TleLine1 = tleLine1;
            existing.TleLine2 = tleLine2;
            existing.LastUpdate = DateTime.UtcNow;

            _dbContext.Satellites.Update(existing);
            await _dbContext.SaveChangesAsync();
        }
    }
}
