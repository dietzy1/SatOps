
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
// Import db context

namespace SatOps.Modules.Groundstation
{
    public interface IGroundStationRepository
    {
        Task<List<GroundStation>> GetAllAsync();
        Task<GroundStation?> GetByIdAsync(int id);
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

        public async Task<GroundStation> AddAsync(GroundStation entity)
        {
            _dbContext.GroundStations.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<GroundStation?> UpdateAsync(GroundStation entity)
        {
            var existing = await _dbContext.GroundStations.FirstOrDefaultAsync(g => g.Id == entity.Id);
            if (existing == null)
            {
                return null;
            }

            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await _dbContext.SaveChangesAsync();
            return existing;
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

