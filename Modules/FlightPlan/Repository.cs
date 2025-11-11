using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.FlightPlan
{
    public interface IFlightPlanRepository
    {
        Task<FlightPlan> AddAsync(FlightPlan entity);
        Task<FlightPlan?> GetByIdAsync(int id); // For updates (tracked)
        Task<FlightPlan?> GetByIdReadOnlyAsync(int id); // For reads (untracked)
        Task<List<FlightPlan>> GetAllAsync();
        Task UpdateAsync(FlightPlan entity);
        Task<List<FlightPlan>> GetPlansReadyForTransmissionAsync(DateTime horizon);
    }

    public class FlightPlanRepository(SatOpsDbContext dbContext) : IFlightPlanRepository
    {
        public Task<List<FlightPlan>> GetAllAsync()
        {
            return dbContext.FlightPlans.AsNoTracking()
                .OrderByDescending(fp => fp.CreatedAt).ToListAsync();
        }

        // When modifying entity
        public Task<FlightPlan?> GetByIdAsync(int id)
        {
            return dbContext.FlightPlans.FirstOrDefaultAsync(fp => fp.Id == id);
        }

        // When only reading entity
        public Task<FlightPlan?> GetByIdReadOnlyAsync(int id)
        {
            return dbContext.FlightPlans.AsNoTracking().FirstOrDefaultAsync(fp => fp.Id == id);
        }

        public async Task<FlightPlan> AddAsync(FlightPlan entity)
        {
            dbContext.FlightPlans.Add(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(FlightPlan entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            dbContext.FlightPlans.Update(entity);
            await dbContext.SaveChangesAsync();
        }

        public Task<List<FlightPlan>> GetPlansReadyForTransmissionAsync(DateTime horizon)
        {
            // Find plans that are ready and scheduled to happen between now and the 'horizon' time.
            return dbContext.FlightPlans
                .AsNoTracking()
                .Where(fp => fp.Status == FlightPlanStatus.AssignedToOverpass &&
                             fp.ScheduledAt.HasValue &&
                             fp.ScheduledAt.Value <= horizon)
                .ToListAsync();
        }
    }
}