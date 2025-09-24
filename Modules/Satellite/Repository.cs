using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.Satellite
{
    public interface ISatelliteRepository
    {
        Task<List<Satellite>> GetAllAsync();
        Task<Satellite?> GetByIdAsync(int id);
        Task<Satellite?> UpdateAsync(Satellite entity);

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

        public async Task<Satellite?> UpdateAsync(Satellite entity)
        {
            var existing = await _dbContext.Satellites.FirstOrDefaultAsync(s => s.Id == entity.Id);
            if (existing == null) return null;

            entity.UpdatedAt = DateTime.UtcNow;
            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await _dbContext.SaveChangesAsync();
            return existing;
        }
    }
}
