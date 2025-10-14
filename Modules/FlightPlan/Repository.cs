using Microsoft.EntityFrameworkCore;
using SatOps.Data;

namespace SatOps.Modules.FlightPlan
{
    public interface IFlightPlanRepository
    {
        Task<FlightPlan> AddAsync(FlightPlan entity);
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<List<FlightPlan>> GetAllAsync();
        Task UpdateAsync(FlightPlan entity);
        Task DeleteAsync(int id);
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