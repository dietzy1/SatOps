using System.Text.Json;
using SatOps.Data;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;


/*\n * Flight Plan Workflow Implementation:\n * \n * 1. Draft: Flight plan created without overpass association\n * 2. ApprovedAwaitingOverpass: Flight plan approved but not yet associated with overpass\n * 3. Approved: Flight plan approved and associated with specific overpass\n * 4. Rejected: Flight plan rejected during review\n * 5. Transmitted: Flight plan sent to satellite (immutable after this point)\n * 6. Superseded: Old version when new version is created\n * \n * - Flight plans must be approved before they can be associated with overpasses\n * - Overpass association calculates suitable overpasses from provided timerange\n * - Versioning ensures full audit trail via PreviousPlanId linking\n */

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status);
        Task<(bool Success, string Message)> AssociateWithOverpassAsync(int flightPlanId, OverpassWindowsCalculationRequestDto overpassRequest);
    }

    public class FlightPlanService : IFlightPlanService
    {
        private readonly IFlightPlanRepository _repository;
        private readonly SatOpsDbContext _dbContext;
        private readonly ISatelliteService _satelliteService;
        private readonly IGroundStationService _groundStationService;
        private readonly IService _overpassService;

        public FlightPlanService(IFlightPlanRepository repository, SatOpsDbContext dbContext,
            ISatelliteService satelliteService, IGroundStationService groundStationService, IService overpassService)
        {
            _repository = repository;
            _dbContext = dbContext;
            _satelliteService = satelliteService;
            _groundStationService = groundStationService;
            _overpassService = overpassService;
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
                Status = FlightPlanStatus.Draft, // Start in draft status
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
                if (oldPlan == null || (oldPlan.Status != FlightPlanStatus.Draft && oldPlan.Status != FlightPlanStatus.ApprovedAwaitingOverpass && oldPlan.Status != FlightPlanStatus.Approved))
                {
                    return null;
                }

                oldPlan.Status = FlightPlanStatus.Superseded;
                oldPlan.UpdatedAt = DateTime.UtcNow;

                var newPlan = new FlightPlan
                {
                    Name = updateDto.FlightPlanBody.Name,
                    Body = JsonDocument.Parse(JsonSerializer.Serialize(updateDto.FlightPlanBody.Body)),
                    ScheduledAt = updateDto.ScheduledAt,
                    GroundStationId = updateDto.GsId,
                    SatelliteId = updateDto.SatId,
                    Status = oldPlan.Status == FlightPlanStatus.Draft ? FlightPlanStatus.Draft : oldPlan.Status, // Preserve current status
                    OverpassId = oldPlan.OverpassId, // Preserve overpass association
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
            if (plan.Status != FlightPlanStatus.Draft)
            {
                return (false, $"Cannot approve a plan with status '{plan.Status}'. Only draft flight plans can be approved.");
            }

            if (status.ToLower() == "approved")
            {
                plan.Status = FlightPlanStatus.ApprovedAwaitingOverpass;
            }
            else if (status.ToLower() == "rejected")
            {
                plan.Status = FlightPlanStatus.Rejected;
            }
            else
            {
                return (false, "Invalid status. Must be 'approved' or 'rejected'.");
            }

            plan.ApprovalDate = DateTime.UtcNow;
            plan.ApproverId = "mock-user-id";

            var success = await _repository.UpdateAsync(plan);

            if (success)
            {
                return (true, $"Flight plan successfully {status}.");
            }

            return (false, "Failed to update the flight plan.");
        }

        public async Task<(bool Success, string Message)> AssociateWithOverpassAsync(int flightPlanId, OverpassWindowsCalculationRequestDto overpassRequest)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var flightPlan = await _repository.GetByIdAsync(flightPlanId);
                if (flightPlan == null)
                {
                    return (false, "Flight plan not found.");
                }

                if (flightPlan.Status != FlightPlanStatus.ApprovedAwaitingOverpass)
                {
                    return (false, $"Cannot associate overpass with a flight plan in '{flightPlan.Status}' status. Flight plan must be approved first.");
                }

                // Validate that the overpass request matches the flight plan's satellite and ground station
                if (overpassRequest.SatelliteId != flightPlan.SatelliteId)
                {
                    return (false, "Overpass satellite does not match flight plan satellite.");
                }

                if (overpassRequest.GroundStationId != flightPlan.GroundStationId)
                {
                    return (false, "Overpass ground station does not match flight plan ground station.");
                }

                // Calculate overpasses within the specified timerange
                var availableOverpasses = await _overpassService.CalculateOverpassesAsync(overpassRequest);
                if (!availableOverpasses.Any())
                {
                    return (false, "No suitable overpasses found within the specified timerange.");
                }

                // Use the first available overpass (could be enhanced to allow selection)
                var selectedOverpass = availableOverpasses.First();

                // Find or create the overpass in storage
                var storedOverpass = await _overpassService.FindOrCreateOverpassAsync(selectedOverpass);
                if (storedOverpass == null)
                {
                    return (false, "Failed to store overpass data.");
                }

                // Associate the flight plan with the stored overpass and finalize approval
                flightPlan.OverpassId = storedOverpass.Id;
                flightPlan.ScheduledAt = selectedOverpass.StartTime;
                flightPlan.Status = FlightPlanStatus.Approved;
                flightPlan.UpdatedAt = DateTime.UtcNow;

                var success = await _repository.UpdateAsync(flightPlan);

                if (success)
                {
                    await transaction.CommitAsync();
                    return (true, $"Flight plan successfully associated with overpass starting at {selectedOverpass.StartTime:yyyy-MM-dd HH:mm:ss} UTC.");
                }

                await transaction.RollbackAsync();
                return (false, "Failed to update the flight plan.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error associating flight plan with overpass: {ex.Message}");
            }
        }
    }
}