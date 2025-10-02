using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Overpass
{
    public interface IOverpassRepository
    {
        Task<Entity?> GetByIdAsync(int id);
        Task<Entity?> GetByIdReadOnlyAsync(int id);
        Task<List<Entity>> GetAllAsync();
        Task<Entity> AddAsync(Entity overpass);
        Task<bool> UpdateAsync(Entity overpass);
        Task<bool> DeleteAsync(int id);
        Task<List<Entity>> GetByTimeRangeAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime);
        Task<Entity?> FindExistingOverpassAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime, double maxElevation);
        Task<List<Entity>> FindStoredOverpassesInTimeRange(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime);
        Task<SatOps.Modules.Schedule.FlightPlan?> GetAssociatedFlightPlanAsync(int overpassId);
    }

    public class OverpassRepository : IOverpassRepository
    {
        private readonly SatOpsDbContext _dbContext;

        public OverpassRepository(SatOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Entity?> GetByIdAsync(int id)
        {
            return await _dbContext.Overpasses.FindAsync(id);
        }

        public async Task<Entity?> GetByIdReadOnlyAsync(int id)
        {
            return await _dbContext.Overpasses.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Entity>> GetAllAsync()
        {
            return await _dbContext.Overpasses.AsNoTracking().ToListAsync();
        }

        public async Task<Entity> AddAsync(Entity overpass)
        {
            _dbContext.Overpasses.Add(overpass);
            await _dbContext.SaveChangesAsync();
            return overpass;
        }

        public async Task<bool> UpdateAsync(Entity overpass)
        {
            try
            {
                _dbContext.Overpasses.Update(overpass);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var overpass = await GetByIdAsync(id);
            if (overpass == null)
                return false;

            _dbContext.Overpasses.Remove(overpass);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<Entity>> GetByTimeRangeAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime)
        {
            return await _dbContext.Overpasses
                .AsNoTracking()
                .Where(o => o.SatelliteId == satelliteId &&
                           o.GroundStationId == groundStationId &&
                           o.StartTime >= startTime &&
                           o.EndTime <= endTime)
                .OrderBy(o => o.StartTime)
                .ToListAsync();
        }

        public async Task<Entity?> FindExistingOverpassAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime, double maxElevation)
        {
            // Find an existing overpass that matches the parameters (within a small tolerance)
            var toleranceMinutes = 5; // 5-minute tolerance

            return await _dbContext.Overpasses
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SatelliteId == satelliteId &&
                                         o.GroundStationId == groundStationId &&
                                         Math.Abs((o.StartTime - startTime).TotalMinutes) < toleranceMinutes &&
                                         Math.Abs((o.EndTime - endTime).TotalMinutes) < toleranceMinutes &&
                                         Math.Abs(o.MaxElevation - maxElevation) < 1.0); // 1 degree tolerance
        }

        public async Task<List<Entity>> FindStoredOverpassesInTimeRange(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime)
        {
            return await _dbContext.Overpasses
                .AsNoTracking()
                .Where(o => o.SatelliteId == satelliteId &&
                           o.GroundStationId == groundStationId &&
                           ((o.StartTime >= startTime && o.StartTime <= endTime) ||
                            (o.EndTime >= startTime && o.EndTime <= endTime) ||
                            (o.StartTime <= startTime && o.EndTime >= endTime)))
                .OrderBy(o => o.StartTime)
                .ToListAsync();
        }

        public async Task<SatOps.Modules.Schedule.FlightPlan?> GetAssociatedFlightPlanAsync(int overpassId)
        {
            return await _dbContext.FlightPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(fp => fp.OverpassId == overpassId);
        }
    }
}