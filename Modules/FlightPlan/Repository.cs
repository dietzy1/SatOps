using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanRepository
    {
        Task<List<FlightPlan>> GetAllAsync();
        Task<FlightPlan?> GetByIdAsync(int id); // For updates (tracked)
        Task<FlightPlan?> GetByIdReadOnlyAsync(int id); // For reads (untracked)
        Task<FlightPlan> AddAsync(FlightPlan entity);
        Task<bool> UpdateAsync(FlightPlan entity);
    }

    public class FlightPlanRepository : IFlightPlanRepository
    {
        private readonly SatOpsDbContext _dbContext;

        public FlightPlanRepository(SatOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<FlightPlan>> GetAllAsync()
        {
            return _dbContext.FlightPlans.AsNoTracking()
                .OrderByDescending(fp => fp.CreatedAt).ToListAsync();
        }

        // When modifying entity
        public Task<FlightPlan?> GetByIdAsync(int id)
        {
            return _dbContext.FlightPlans.FirstOrDefaultAsync(fp => fp.Id == id);
        }

        // When only reading entity
        public Task<FlightPlan?> GetByIdReadOnlyAsync(int id)
        {
            return _dbContext.FlightPlans.AsNoTracking().FirstOrDefaultAsync(fp => fp.Id == id);
        }

        public async Task<FlightPlan> AddAsync(FlightPlan entity)
        {
            _dbContext.FlightPlans.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(FlightPlan entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _dbContext.FlightPlans.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}