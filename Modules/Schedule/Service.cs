using System.Text.Json;
using SatOps.Modules.Schedule;

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(Guid id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(Guid id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(Guid id, string status);
    }

    public class FlightPlanService : IFlightPlanService
    {
        private readonly IFlightPlanRepository _repository;
        private readonly SatOpsDbContext _dbContext;

        public FlightPlanService(IFlightPlanRepository repository, SatOpsDbContext dbContext)
        {
            _repository = repository;
            _dbContext = dbContext;
        }

        public Task<List<FlightPlan>> ListAsync() => _repository.GetAllAsync();

        public Task<FlightPlan?> GetByIdAsync(Guid id) => _repository.GetByIdReadOnlyAsync(id);

        public Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto)
        {
            if (!Guid.TryParse(createDto.GsId, out var groundStationId))
            {
                throw new ArgumentException("Invalid Ground Station ID format.", nameof(createDto.GsId));
            }

            var entity = new FlightPlan
            {
                Name = createDto.FlightPlanBody.Name,
                Body = JsonDocument.Parse(JsonSerializer.Serialize(createDto.FlightPlanBody.Body)),
                ScheduledAt = createDto.ScheduledAt,
                GroundStationId = groundStationId,
                SatelliteName = createDto.SatName,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return _repository.AddAsync(entity);
        }

        public async Task<FlightPlan?> CreateNewVersionAsync(Guid id, CreateFlightPlanDto updateDto)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (!Guid.TryParse(updateDto.GsId, out var groundStationId))
                {
                    throw new ArgumentException("Invalid Ground Station ID format.", nameof(updateDto.GsId));
                }

                var oldPlan = await _repository.GetByIdAsync(id);
                if (oldPlan == null || oldPlan.Status != "pending")
                {
                    return null;
                }

                oldPlan.Status = "superseded";
                oldPlan.UpdatedAt = DateTime.UtcNow;

                var newPlan = new FlightPlan
                {
                    Name = updateDto.FlightPlanBody.Name,
                    Body = JsonDocument.Parse(JsonSerializer.Serialize(updateDto.FlightPlanBody.Body)),
                    ScheduledAt = updateDto.ScheduledAt,
                    GroundStationId = groundStationId,
                    SatelliteName = updateDto.SatName,
                    Status = "pending",
                    PreviousPlanId = oldPlan.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(newPlan);
                await _repository.UpdateAsync(oldPlan);

                await transaction.CommitAsync();

                return newPlan;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string Message)> ApproveOrRejectAsync(Guid id, string status)
        {
            var plan = await _repository.GetByIdAsync(id);
            if (plan == null)
            {
                return (false, "Flight plan not found.");
            }
            if (plan.Status != "pending")
            {
                return (false, $"Cannot update a plan with status '{plan.Status}'.");
            }

            plan.Status = status;
            plan.ApprovalDate = DateTime.UtcNow;
            plan.ApproverId = "mock-user-id";

            var success = await _repository.UpdateAsync(plan);

            if (success)
            {
                return (true, $"Flight plan successfully {status}.");
            }

            return (false, "Failed to update the flight plan.");
        }
    }
}