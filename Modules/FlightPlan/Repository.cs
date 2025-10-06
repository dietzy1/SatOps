using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanRepository
    {
        Task<FlightPlan> AddAsync(FlightPlan entity);
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<List<FlightPlan>> GetAllAsync();
        Task UpdateAsync(FlightPlan entity);
        Task DeleteAsync(int id);
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



        public async Task<FlightPlan> AddAsync(FlightPlan entity)
        {
            _dbContext.FlightPlans.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(FlightPlan entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _dbContext.FlightPlans.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbContext.FlightPlans.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}