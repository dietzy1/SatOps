using System.Text.Json;
using SatOps.Data;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;


// We need to create some relation between a flight plan and a overpass
// Overpasses are generated on the fly so how do we link them? This is primarily to ensure that we dont overwhelm a groundstation with too many flight plans and for traceability
// This also means that we need to start storing flight plan when associating them with overpasses
// Maybe we can store the overpass id in the flight plan when we create it? We should probaly have a multistep process for creating a flight plan
// 1. Create a flight plan with status "draft" and no overpass id
// 2. Associate the flight plan with an overpass and set the status to "pending" // When its associated then the overpass is saved in its own service
// 3. Approve the flight plan and set the status to "approved"
// 4. Transmit the flight plan and set the status to "transmitted"

// Do we need immutability for the flight plans?

// Simple approach is just to make it so a flight can be updated if its in pending/draft/approved state must when its transmitted it cannot be updated
// It cannot be updated after its transmitted

// Harder approach is to make it so a flight plan cannot be updated once its created, instead a new version must be created
// This is probably the better approach as it ensures that we have a full history of all changes
// We can link the new version to the old version via a previous_plan_id field

// We should add a command enum which defines what commands the satelite should execute
// 1. Take picture
// 2. Start telemetry downlink

// No instead of a command enum we should have an array of commands that can be executed in sequence
// These commands can have calculators for example for taking a picture we can have a calculator that defines when to take the picture



public enum FlightPlanStatus
{
    Draft,
    Pending,
    Approved,
    Rejected,
    Superseded,
    Transmitted
}

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status);
    }

    public class FlightPlanService : IFlightPlanService
    {
        private readonly IFlightPlanRepository _repository;
        private readonly SatOpsDbContext _dbContext;
        private readonly ISatelliteService _satelliteService;
        private readonly IGroundStationService _groundStationService;

        public FlightPlanService(IFlightPlanRepository repository, SatOpsDbContext dbContext,
            ISatelliteService satelliteService, IGroundStationService groundStationService)
        {
            _repository = repository;
            _dbContext = dbContext;
            _satelliteService = satelliteService;
            _groundStationService = groundStationService;
        }

        public Task<List<FlightPlan>> ListAsync() => _repository.GetAllAsync();

        public Task<FlightPlan?> GetByIdAsync(int id) => _repository.GetByIdReadOnlyAsync(id);

        public async Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto)
        {
            // Validate that the groundstation exists
            var groundStation = await _groundStationService.GetAsync(createDto.GsId);
            if (groundStation == null)
            {
                throw new ArgumentException($"Ground station with ID {createDto.GsId} not found.", nameof(createDto.GsId));
            }

            // Validate that the satellite exists
            var satellite = await _satelliteService.GetAsync(createDto.SatId);
            if (satellite == null)
            {
                throw new ArgumentException($"Satellite with ID {createDto.SatId} not found.", nameof(createDto.SatId));
            }

            var entity = new FlightPlan
            {
                Name = createDto.FlightPlanBody.Name,
                Body = JsonDocument.Parse(JsonSerializer.Serialize(createDto.FlightPlanBody.Body)),
                ScheduledAt = createDto.ScheduledAt,
                GroundStationId = createDto.GsId,
                SatelliteId = createDto.SatId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repository.AddAsync(entity);
        }

        public async Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Validate that the groundstation exists
                var groundStation = await _groundStationService.GetAsync(updateDto.GsId);
                if (groundStation == null)
                {
                    throw new ArgumentException($"Ground station with ID {updateDto.GsId} not found.", nameof(updateDto.GsId));
                }

                // Validate that the satellite exists
                var satellite = await _satelliteService.GetAsync(updateDto.SatId);
                if (satellite == null)
                {
                    throw new ArgumentException($"Satellite with ID {updateDto.SatId} not found.", nameof(updateDto.SatId));
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
                    GroundStationId = updateDto.GsId,
                    SatelliteId = updateDto.SatId,
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

        public async Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status)
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