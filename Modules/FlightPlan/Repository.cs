using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.FlightPlan
{
    public interface IFlightPlanRepository
    {
        Task<List<FlightPlan>> GetAllAsync();
        Task<FlightPlan?> GetByIdAsync(int id); // For updates (tracked)
        Task<FlightPlan?> GetByIdReadOnlyAsync(int id); // For reads (untracked)
        Task<FlightPlan> AddAsync(FlightPlan entity);
        Task<bool> UpdateAsync(FlightPlan entity);
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

        public async Task<bool> UpdateAsync(FlightPlan entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            dbContext.FlightPlans.Update(entity);
            return await dbContext.SaveChangesAsync() > 0;
        }
    }
}