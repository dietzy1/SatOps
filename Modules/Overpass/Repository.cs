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
        Task<Entity?> FindOverpassInTimeWindowAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime, int toleranceMinutes);
        Task<List<Entity>> FindStoredOverpassesInTimeRange(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime);
        Task<FlightPlan.FlightPlan?> GetAssociatedFlightPlanAsync(int overpassId);
    }

    public class OverpassRepository(SatOpsDbContext dbContext) : IOverpassRepository
    {
        public async Task<Entity?> GetByIdAsync(int id)
        {
            return await dbContext.Overpasses.FindAsync(id);
        }

        public async Task<Entity?> GetByIdReadOnlyAsync(int id)
        {
            return await dbContext.Overpasses.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Entity>> GetAllAsync()
        {
            return await dbContext.Overpasses.AsNoTracking().ToListAsync();
        }

        public async Task<Entity> AddAsync(Entity overpass)
        {
            dbContext.Overpasses.Add(overpass);
            await dbContext.SaveChangesAsync();
            return overpass;
        }

        public async Task<bool> UpdateAsync(Entity overpass)
        {
            try
            {
                dbContext.Overpasses.Update(overpass);
                await dbContext.SaveChangesAsync();
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

            dbContext.Overpasses.Remove(overpass);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<Entity>> GetByTimeRangeAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime)
        {
            return await dbContext.Overpasses
                .AsNoTracking()
                .Where(o => o.SatelliteId == satelliteId &&
                           o.GroundStationId == groundStationId &&
                           o.StartTime >= startTime &&
                           o.EndTime <= endTime)
                .OrderBy(o => o.StartTime)
                .ToListAsync();
        }

        public async Task<Entity?> FindOverpassInTimeWindowAsync(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime, int toleranceMinutes)
        {
            // Find any existing overpass (assigned or not) that overlaps with the requested time window
            // This prevents creating duplicate overpass records for the same physical satellite pass
            // Uses tolerance to account for TLE data variations between calculations

            return await dbContext.Overpasses
                .Include(o => o.FlightPlan)
                .Where(o => o.SatelliteId == satelliteId &&
                           o.GroundStationId == groundStationId &&
                           Math.Abs((o.StartTime - startTime).TotalMinutes) <= toleranceMinutes &&
                           Math.Abs((o.EndTime - endTime).TotalMinutes) <= toleranceMinutes)
                .OrderBy(o => Math.Abs((o.StartTime - startTime).TotalMinutes) + Math.Abs((o.EndTime - endTime).TotalMinutes))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Entity>> FindStoredOverpassesInTimeRange(int satelliteId, int groundStationId, DateTime startTime, DateTime endTime)
        {
            return await dbContext.Overpasses
                .AsNoTracking()
                .Where(o => o.SatelliteId == satelliteId &&
                           o.GroundStationId == groundStationId &&
                           ((o.StartTime >= startTime && o.StartTime <= endTime) ||
                            (o.EndTime >= startTime && o.EndTime <= endTime) ||
                            (o.StartTime <= startTime && o.EndTime >= endTime)))
                .OrderBy(o => o.StartTime)
                .ToListAsync();
        }

        public async Task<FlightPlan.FlightPlan?> GetAssociatedFlightPlanAsync(int overpassId)
        {
            var overpass = await dbContext.Overpasses
                .AsNoTracking()
                .Include(o => o.FlightPlan)
                .FirstOrDefaultAsync(o => o.Id == overpassId);

            return overpass?.FlightPlan;
        }
    }
}