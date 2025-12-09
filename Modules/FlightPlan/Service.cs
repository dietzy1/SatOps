using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SatOps.Modules.User;
using Microsoft.Extensions.Options;
using SatOps.Configuration;

namespace SatOps.Modules.FlightPlan
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status);
        Task<(bool Success, string Message)> AssignOverpassAsync(int flightPlanId, AssignOverpassDto overpassRequest);
        Task<List<string>> CompileFlightPlanToCshAsync(int id);
        Task<ImagingTimingResponseDto> GetImagingOpportunity(int satelliteId, double targetLatitude, double targetLongitude, DateTime? commandReceptionTime = null);
        Task UpdateFlightPlanStatusAsync(int flightPlanId, FlightPlanStatus newStatus, string? failureReason = null);
        Task<List<FlightPlan>> GetPlansReadyForTransmissionAsync(TimeSpan lookahead);
    }

    public class FlightPlanService(
        IFlightPlanRepository repository,
        ISatelliteService satelliteService,
        IGroundStationService groundStationService,
        IOverpassService overpassService,
        IImagingCalculation imagingCalculation,
        ICurrentUserProvider currentUserProvider,
        IOptions<ImagingCalculationOptions> imagingOptions,
        ILogger<FlightPlanService> logger
    ) : IFlightPlanService
    {
        public Task<List<FlightPlan>> ListAsync() => repository.GetAllAsync();

        public Task<FlightPlan?> GetByIdAsync(int id) => repository.GetByIdReadOnlyAsync(id);
        public async Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto)
        {
            var currentUserId = currentUserProvider.GetUserId();
            if (currentUserId == null) throw new InvalidOperationException("User ID claim not found.");

            var groundStation = await groundStationService.GetAsync(createDto.GsId);
            if (groundStation == null) throw new ArgumentException($"Ground station with ID {createDto.GsId} not found.", nameof(createDto.GsId));

            var satellite = await satelliteService.GetAsync(createDto.SatId);
            if (satellite == null) throw new ArgumentException($"Satellite with ID {createDto.SatId} not found.", nameof(createDto.SatId));

            var (isValid, errors) = createDto.Commands.ValidateAll();
            if (!isValid) throw new ArgumentException($"Command validation failed: {string.Join("; ", errors)}");

            var entity = new FlightPlan
            {
                Name = createDto.Name,
                GroundStationId = createDto.GsId,
                SatelliteId = createDto.SatId,
                Status = FlightPlanStatus.Draft,
                CreatedById = currentUserId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            entity.SetCommands(createDto.Commands);
            return await repository.AddAsync(entity);
        }

        public async Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto)
        {
            var existing = await repository.GetByIdAsync(id);
            if (existing == null) return null;

            if (existing.Status != FlightPlanStatus.Draft &&
                existing.Status != FlightPlanStatus.Approved &&
                existing.Status != FlightPlanStatus.AssignedToOverpass)
            {
                return null;
            }

            var groundStation = await groundStationService.GetAsync(updateDto.GsId);
            if (groundStation == null) throw new ArgumentException($"Ground station with ID {updateDto.GsId} not found.", nameof(updateDto.GsId));

            var satellite = await satelliteService.GetAsync(updateDto.SatId);
            if (satellite == null) throw new ArgumentException($"Satellite with ID {updateDto.SatId} not found.", nameof(updateDto.SatId));

            var (isValid, errors) = updateDto.Commands.ValidateAll();
            if (!isValid) throw new ArgumentException($"Command validation failed: {string.Join("; ", errors)}");

            existing.Status = FlightPlanStatus.Superseded;
            existing.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(existing);

            var newVersion = new FlightPlan
            {
                Name = updateDto.Name,
                GroundStationId = updateDto.GsId,
                SatelliteId = updateDto.SatId,
                PreviousPlanId = existing.Id,
                Status = FlightPlanStatus.Draft,
                CreatedById = existing.CreatedById,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            newVersion.SetCommands(updateDto.Commands);
            return await repository.AddAsync(newVersion);
        }

        public async Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status)
        {
            var currentUserId = currentUserProvider.GetUserId();
            if (currentUserId == null) throw new InvalidOperationException("User ID claim not found.");

            var plan = await repository.GetByIdAsync(id);
            if (plan == null) return (false, "Flight plan not found.");

            switch (plan.Status)
            {
                case FlightPlanStatus.Draft: break;
                case FlightPlanStatus.Rejected: return (false, "Cannot modify a plan that has already been rejected.");
                case FlightPlanStatus.Approved: return (false, "Cannot modify a plan that has already been approved.");
                case FlightPlanStatus.AssignedToOverpass: return (false, "Cannot modify a plan that has been assigned to an overpass.");
                case FlightPlanStatus.Transmitted: return (false, "Cannot modify a plan that has been transmitted.");
                case FlightPlanStatus.Superseded: return (false, "Cannot modify a superseded plan.");
                default: return (false, $"Unknown flight plan status: {plan.Status}");
            }

            var newStatus = FlightPlanStatusExtensions.FromScreamCase(status);

            if (newStatus == FlightPlanStatus.Approved)
            {
                var commands = plan.GetCommands();
                var (isValid, errors) = commands.ValidateAll();
                if (!isValid) return (false, $"Cannot approve invalid flight plan: {string.Join("; ", errors)}");
            }

            plan.Status = newStatus;
            plan.ApprovalDate = DateTime.UtcNow;
            plan.ApprovedById = currentUserId;

            await repository.UpdateAsync(plan);
            return (true, $"Flight plan {status.ToLowerInvariant()} successfully");
        }

        public async Task<(bool Success, string Message)> AssignOverpassAsync(
            int id,
            AssignOverpassDto dto)
        {
            try
            {
                var flightPlan = await repository.GetByIdAsync(id);
                if (flightPlan == null) return (false, "Flight plan not found.");

                if (dto.StartTime < DateTime.UtcNow) return (false, "Cannot assign an overpass that starts in the past.");
                if (dto.EndTime < DateTime.UtcNow) return (false, "Cannot assign an overpass that ends in the past.");
                if (dto.StartTime >= dto.EndTime) return (false, "Start time must be before end time.");

                if (flightPlan.Status != FlightPlanStatus.Approved)
                {
                    return (false, $"Flight plan must be in APPROVED status to assign an overpass. Current: {flightPlan.Status}");
                }

                if (!flightPlan.GroundStationId.HasValue)
                {
                    return (false, "Flight plan has no assigned ground station.");
                }

                var satellite = await satelliteService.GetAsync(flightPlan.SatelliteId);
                if (satellite == null) return (false, "Associated satellite not found.");

                var toleranceMinutes = 30;
                var expandedStartTime = dto.StartTime.AddMinutes(-toleranceMinutes);
                var expandedEndTime = dto.EndTime.AddMinutes(toleranceMinutes);

                var availableOverpasses = await overpassService.CalculateOverpassesAsync(new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = flightPlan.SatelliteId,
                    GroundStationId = flightPlan.GroundStationId.Value,
                    StartTime = expandedStartTime,
                    EndTime = expandedEndTime,
                });

                if (availableOverpasses == null || availableOverpasses.Count == 0)
                    return (false, "No matching overpass found in the specified time window.");

                var matchToleranceMinutes = 15;
                OverpassWindowDto? selectedOverpass = null;
                double bestScore = double.MaxValue;

                foreach (var candidate in availableOverpasses)
                {
                    var startTimeDiff = Math.Abs((candidate.StartTime - dto.StartTime).TotalMinutes);
                    var endTimeDiff = Math.Abs((candidate.EndTime - dto.EndTime).TotalMinutes);

                    if (startTimeDiff > matchToleranceMinutes || endTimeDiff > matchToleranceMinutes) continue;

                    double elevationDiff = dto.MaxElevation.HasValue ? Math.Abs(candidate.MaxElevation - dto.MaxElevation.Value) : 0;
                    double durationDiff = dto.DurationSeconds.HasValue ? Math.Abs(candidate.DurationSeconds - dto.DurationSeconds.Value) : 0;

                    var score = (startTimeDiff * 2.0) + (endTimeDiff * 2.0) + (elevationDiff * 0.5) + (durationDiff / 60.0 * 0.5);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        selectedOverpass = candidate;
                    }
                }

                if (selectedOverpass == null)
                    return (false, $"No overpass found within {matchToleranceMinutes}-minute tolerance.");

                // Check for overpass conflicts: prevent two ground stations from uploading to the same satellite
                // at overlapping times. This prevents race conditions where nearby ground stations could
                // both attempt to communicate with the satellite simultaneously.
                var overpassConflictMargin = TimeSpan.FromMinutes(5);
                var activePlans = await repository.GetActivePlansBySatelliteAsync(flightPlan.SatelliteId);

                foreach (var activePlan in activePlans)
                {
                    if (activePlan.Id == flightPlan.Id) continue;
                    if (!activePlan.ScheduledAt.HasValue) continue;

                    // Check if the scheduled transmission times overlap (with margin)
                    var timeDiff = Math.Abs((selectedOverpass.MaxElevationTime - activePlan.ScheduledAt.Value).TotalSeconds);

                    if (timeDiff < overpassConflictMargin.TotalSeconds)
                    {
                        return (false, $"Overpass Conflict: This overpass conflicts with Flight Plan #{activePlan.Id} ('{activePlan.Name}'). " +
                                       $"Scheduled transmission times overlap at {selectedOverpass.MaxElevationTime:O} vs {activePlan.ScheduledAt.Value:O} " +
                                       $"(Margin: {overpassConflictMargin.TotalMinutes} min). Choose a different overpass window.");
                    }
                }

                // Note: Execution time calculation and conflict checking for command overlap is deferred
                // to transmission time in CompileFlightPlanToCshAsync. This avoids duplicate calculations
                // and ensures we use the most up-to-date TLE data when the plan is actually transmitted.

                var (success, overpassEntity, message) = await overpassService.FindOrCreateOverpassForFlightPlanAsync(
                    selectedOverpass,
                    flightPlan.Id,
                    matchToleranceMinutes,
                    satellite.TleLine1,
                    satellite.TleLine2,
                    DateTime.UtcNow
                );

                if (!success || overpassEntity == null) return (false, message);

                flightPlan.Status = FlightPlanStatus.AssignedToOverpass;
                flightPlan.ScheduledAt = selectedOverpass.MaxElevationTime;
                flightPlan.UpdatedAt = DateTime.UtcNow;

                await repository.UpdateAsync(flightPlan);

                return (true, $"Flight plan successfully assigned to overpass (ID: {overpassEntity.Id}).");
            }
            catch (ArgumentException ex)
            {
                return (false, $"Invalid data: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return (false, $"Operation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<List<string>> CompileFlightPlanToCshAsync(int flightPlanId)
        {
            logger.LogInformation("Compiling flight plan {FlightPlanId} to CSH", flightPlanId);

            var flightPlan = await repository.GetByIdAsync(flightPlanId);
            if (flightPlan == null) throw new ArgumentException($"Flight plan with ID {flightPlanId} not found.");

            var satellite = await satelliteService.GetAsync(flightPlan.SatelliteId);
            if (satellite == null) throw new ArgumentException($"Satellite with ID {flightPlan.SatelliteId} not found.");

            var commands = flightPlan.GetCommands();
            var (isValid, errors) = commands.ValidateAll();
            if (!isValid) throw new InvalidOperationException($"Cannot compile invalid flight plan. Errors: {string.Join("; ", errors)}");

            // Collect blocked times from transmitted flight plans for this satellite.
            // Only transmitted plans have finalized execution times - plans still in
            // AssignedToOverpass status will calculate their own times when transmitted.
            var conflictMargin = TimeSpan.FromMinutes(2);
            var blockedTimes = new List<DateTime>();
            var transmittedPlans = await repository.GetTransmittedPlansBySatelliteAsync(flightPlan.SatelliteId);

            logger.LogDebug("Found {TransmittedPlanCount} transmitted flight plans for satellite {SatelliteId}",
                transmittedPlans.Count, flightPlan.SatelliteId);

            foreach (var transmittedPlan in transmittedPlans)
            {
                var transmittedCommands = transmittedPlan.GetCommands();
                foreach (var cmd in transmittedCommands)
                {
                    if (cmd.ExecutionTime.HasValue)
                    {
                        blockedTimes.Add(cmd.ExecutionTime.Value);
                        logger.LogDebug("Blocked time from transmitted flight plan {PlanId}: {Time:O}",
                            transmittedPlan.Id, cmd.ExecutionTime.Value);
                    }
                }
            }

            logger.LogInformation("Calculating execution times for {CommandCount} commands with {BlockedCount} blocked time slots",
                commands.Count, blockedTimes.Count);

            try
            {
                await commands.CalculateExecutionTimesAsync(
                    satellite,
                    imagingCalculation,
                    imagingOptions.Value,
                    blockedTimes,
                    conflictMargin,
                    logger);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to calculate execution times for flight plan {FlightPlanId}", flightPlanId);
                throw new InvalidOperationException($"Failed to calculate execution times: {ex.Message}", ex);
            }

            var cshCommands = await commands.CompileAllToCsh();
            logger.LogInformation("Successfully compiled flight plan {FlightPlanId} to {CshCommandCount} CSH commands",
                flightPlanId, cshCommands.Count);

            return cshCommands;
        }

        public async Task<ImagingTimingResponseDto> GetImagingOpportunity(int satelliteId, double targetLatitude, double targetLongitude, DateTime? commandReceptionTime = null)
        {
            var satellite = await satelliteService.GetAsync(satelliteId);
            if (satellite == null) throw new ArgumentException($"Satellite with ID {satelliteId} not found.", nameof(satelliteId));
            if (string.IsNullOrWhiteSpace(satellite.TleLine1) || string.IsNullOrWhiteSpace(satellite.TleLine2))
                throw new ArgumentException($"Satellite with ID {satelliteId} does not have valid TLE data.", nameof(satelliteId));

            var tle = new Tle(satellite.Name, satellite.TleLine1, satellite.TleLine2);
            var sgp4Satellite = new SGPdotNET.Observation.Satellite(tle);
            var tleAge = DateTime.UtcNow - tle.Epoch;

            var targetCoordinate = new GeodeticCoordinate(Angle.FromDegrees(targetLatitude), Angle.FromDegrees(targetLongitude), 0);
            var maxSearchDuration = TimeSpan.FromHours(imagingOptions.Value.MaxSearchDurationHours);

            var imagingOpportunity = imagingCalculation.FindBestImagingOpportunity(
                sgp4Satellite,
                targetCoordinate,
                commandReceptionTime ?? DateTime.UtcNow,
                maxSearchDuration
            );

            var result = new ImagingTimingResponseDto
            {
                Possible = true,
                ImagingTime = imagingOpportunity.ImagingTime,
                OffNadirDegrees = imagingOpportunity.OffNadirDegrees,
                SatelliteAltitudeKm = imagingOpportunity.SatelliteAltitudeKm,
                TleAgeWarning = tleAge.TotalHours > 48,
                TleAgeHours = tleAge.TotalHours,
            };

            if (result.OffNadirDegrees > imagingOptions.Value.MaxOffNadirDegrees)
            {
                result.Possible = false;
                result.Message = $"No imaging opportunity found within the off-nadir limit of {imagingOptions.Value.MaxOffNadirDegrees}° in the next {imagingOptions.Value.MaxSearchDurationHours} hours.";
            }

            return result;
        }

        public async Task UpdateFlightPlanStatusAsync(int flightPlanId, FlightPlanStatus newStatus, string? failureReason = null)
        {
            var plan = await repository.GetByIdAsync(flightPlanId);
            if (plan != null)
            {
                plan.Status = newStatus;
                plan.FailureReason = failureReason;
                await repository.UpdateAsync(plan);
            }
        }

        public Task<List<FlightPlan>> GetPlansReadyForTransmissionAsync(TimeSpan lookahead)
        {
            var horizon = DateTime.UtcNow.Add(lookahead);
            return repository.GetPlansReadyForTransmissionAsync(horizon);
        }
    }
}