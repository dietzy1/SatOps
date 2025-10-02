using System.Text.Json;
using SatOps.Data;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;

namespace SatOps.Modules.Schedule
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status);
        Task<(bool Success, string Message)> AssociateWithOverpassAsync(int flightPlanId, AssociateOverpassDto overpassRequest);
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
                GroundStationId = createDto.GsId,
                SatelliteId = createDto.SatId,
                Status = FlightPlanStatus.Draft,
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
                if (oldPlan == null)
                {
                    return null;
                }

                // Check if the current status allows creating a new version
                bool canCreateNewVersion = oldPlan.Status switch
                {
                    FlightPlanStatus.Draft => true,
                    FlightPlanStatus.Approved => true,
                    FlightPlanStatus.Rejected => false,
                    FlightPlanStatus.AssignedToOverpass => false,
                    FlightPlanStatus.Transmitted => false,
                    FlightPlanStatus.Superseded => false,
                    _ => false
                };

                if (!canCreateNewVersion)
                {
                    return null;
                }

                // Determine the status for the new plan
                var newPlanStatus = oldPlan.Status switch
                {
                    FlightPlanStatus.Draft => FlightPlanStatus.Draft,
                    FlightPlanStatus.Approved => FlightPlanStatus.Draft, // Reset to draft when creating new version from approved plan
                    _ => FlightPlanStatus.Draft
                };

                oldPlan.Status = FlightPlanStatus.Superseded;
                oldPlan.UpdatedAt = DateTime.UtcNow;

                var newPlan = new FlightPlan
                {
                    Name = updateDto.FlightPlanBody.Name,
                    Body = JsonDocument.Parse(JsonSerializer.Serialize(updateDto.FlightPlanBody.Body)),
                    ScheduledAt = null,
                    GroundStationId = updateDto.GsId,
                    SatelliteId = updateDto.SatId,
                    Status = newPlanStatus,
                    OverpassId = oldPlan.Status == FlightPlanStatus.Approved ? oldPlan.OverpassId : null, // Preserve overpass only if previously approved
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

            // Validate current status allows approval/rejection
            switch (plan.Status)
            {
                case FlightPlanStatus.Draft:
                    // Only draft plans can be approved or rejected
                    break;
                case FlightPlanStatus.Rejected:
                    return (false, "Cannot modify a plan that has already been rejected.");
                case FlightPlanStatus.Approved:
                    return (false, "Cannot modify a plan that has already been approved.");
                case FlightPlanStatus.AssignedToOverpass:
                    return (false, "Cannot modify a plan that has been assigned to an overpass.");
                case FlightPlanStatus.Transmitted:
                    return (false, "Cannot modify a plan that has been transmitted.");
                case FlightPlanStatus.Superseded:
                    return (false, "Cannot modify a superseded plan.");
                default:
                    return (false, $"Unknown flight plan status: {plan.Status}");
            }

            // Handle the approval/rejection action
            switch (status.ToLower())
            {
                case "approved":
                    plan.Status = FlightPlanStatus.Approved;
                    break;
                case "rejected":
                    plan.Status = FlightPlanStatus.Rejected;
                    break;
                default:
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

        public async Task<(bool Success, string Message)> AssociateWithOverpassAsync(int flightPlanId, AssociateOverpassDto overpassRequest)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var flightPlan = await _repository.GetByIdAsync(flightPlanId);
                if (flightPlan == null)
                {
                    return (false, "Flight plan not found.");
                }

                // Check if the current status allows association with overpass
                switch (flightPlan.Status)
                {
                    case FlightPlanStatus.Approved:
                        // Only approved plans can be assigned to overpasses
                        break;
                    case FlightPlanStatus.Draft:
                        return (false, "Cannot associate overpass with a draft flight plan. Flight plan must be approved first.");
                    case FlightPlanStatus.Rejected:
                        return (false, "Cannot associate overpass with a rejected flight plan.");
                    case FlightPlanStatus.AssignedToOverpass:
                        return (false, "Flight plan is already assigned to an overpass.");
                    case FlightPlanStatus.Transmitted:
                        return (false, "Cannot modify a transmitted flight plan.");
                    case FlightPlanStatus.Superseded:
                        return (false, "Cannot associate overpass with a superseded flight plan.");
                    default:
                        return (false, $"Unknown flight plan status: {flightPlan.Status}");
                }

                // Calculate overpasses with a broader time window to allow for tolerance matching
                var toleranceHours = 2; // Allow 2-hour tolerance around the requested time window
                var expandedStartTime = overpassRequest.StartTime.AddHours(-toleranceHours);
                var expandedEndTime = overpassRequest.EndTime.AddHours(toleranceHours);

                var overpassCalculationRequest = new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = flightPlan.SatelliteId,
                    GroundStationId = flightPlan.GroundStationId,
                    StartTime = expandedStartTime,
                    EndTime = expandedEndTime,
                };

                var availableOverpasses = await _overpassService.CalculateOverpassesAsync(overpassCalculationRequest);
                if (availableOverpasses.Count.Equals(0))
                {
                    return (false, "No suitable overpasses found within the specified timerange (including tolerance).");
                }

                // Find the overpass that best matches the user's requested criteria
                var toleranceMinutes = 10; // Allow 10-minute tolerance for time matching
                var elevationTolerance = 2.0; // Allow 2-degree tolerance for elevation matching
                var durationTolerance = 60; // Allow 60-second tolerance for duration matching

                var candidateOverpasses = availableOverpasses.Where(o => 
                    // Time window matching (with tolerance)
                    Math.Abs((o.StartTime - overpassRequest.StartTime).TotalMinutes) <= toleranceMinutes ||
                    Math.Abs((o.EndTime - overpassRequest.EndTime).TotalMinutes) <= toleranceMinutes ||
                    (o.StartTime <= overpassRequest.StartTime && o.EndTime >= overpassRequest.EndTime) ||
                    (overpassRequest.StartTime <= o.StartTime && overpassRequest.EndTime >= o.EndTime)
                );

                // Apply additional filtering criteria if provided
                if (overpassRequest.MaxElevation.HasValue)
                {
                    candidateOverpasses = candidateOverpasses.Where(o => 
                        Math.Abs(o.MaxElevation - overpassRequest.MaxElevation.Value) <= elevationTolerance);
                }

                if (overpassRequest.DurationSeconds.HasValue)
                {
                    candidateOverpasses = candidateOverpasses.Where(o => 
                        Math.Abs(o.DurationSeconds - overpassRequest.DurationSeconds.Value) <= durationTolerance);
                }

                if (overpassRequest.MaxElevationTime.HasValue)
                {
                    candidateOverpasses = candidateOverpasses.Where(o => 
                        Math.Abs((o.MaxElevationTime - overpassRequest.MaxElevationTime.Value).TotalMinutes) <= toleranceMinutes);
                }

                // Select the best matching overpass (prioritize by time accuracy, then elevation accuracy)
                var selectedOverpass = candidateOverpasses
                    .OrderBy(o => Math.Abs((o.StartTime - overpassRequest.StartTime).TotalMinutes))
                    .ThenBy(o => overpassRequest.MaxElevation.HasValue ? Math.Abs(o.MaxElevation - overpassRequest.MaxElevation.Value) : 0)
                    .FirstOrDefault();

                if (selectedOverpass == null)
                {
                    var criteriaUsed = new List<string> { "time window" };
                    if (overpassRequest.MaxElevation.HasValue) criteriaUsed.Add("max elevation");
                    if (overpassRequest.DurationSeconds.HasValue) criteriaUsed.Add("duration");
                    if (overpassRequest.MaxElevationTime.HasValue) criteriaUsed.Add("max elevation time");
                    
                    return (false, $"No overpass found matching the specified criteria: {string.Join(", ", criteriaUsed)} (within tolerance).");
                }

                // Store the selected overpass (since we have a 1:1 relationship with flight plans)
                var storedOverpass = await _overpassService.FindOrCreateOverpassAsync(selectedOverpass);
                if (storedOverpass == null)
                {
                    return (false, "Failed to store overpass data.");
                }

                // Associate the flight plan with the stored overpass and update status
                flightPlan.OverpassId = storedOverpass.Id;
                flightPlan.ScheduledAt = selectedOverpass.StartTime;
                flightPlan.Status = FlightPlanStatus.AssignedToOverpass;
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