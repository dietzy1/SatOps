using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.Satellite
{
    public interface ISatelliteRepository
    {
        Task<List<Satellite>> GetAllAsync();
        Task<Satellite?> GetByIdAsync(int id);
        void UpdateAsync(int id, TleDto tleDto);
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

        public async void UpdateAsync(int id, TleDto tleDto)
        {
            var existing = await _dbContext.Satellites.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return;

            existing.TleLine1 = tleDto.TleLine1;
            existing.TleLine2 = tleDto.TleLine2;
            existing.LastUpdate = DateTime.UtcNow;

            _dbContext.Entry(existing).CurrentValues.SetValues(existing);
            await _dbContext.SaveChangesAsync();
            return;
        }
    }
}
