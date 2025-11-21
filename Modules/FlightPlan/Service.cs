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
        IOptions<ImagingCalculationOptions> imagingOptions
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

                var satellite = await satelliteService.GetAsync(flightPlan.SatelliteId);
                if (satellite == null) return (false, "Associated satellite not found.");

                var toleranceMinutes = 30;
                var expandedStartTime = dto.StartTime.AddMinutes(-toleranceMinutes);
                var expandedEndTime = dto.EndTime.AddMinutes(toleranceMinutes);

                var availableOverpasses = await overpassService.CalculateOverpassesAsync(new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = flightPlan.SatelliteId,
                    GroundStationId = flightPlan.GroundStationId,
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

                var currentPlanCommands = flightPlan.GetCommands();

                await currentPlanCommands.CalculateExecutionTimesAsync(satellite, imagingCalculation, imagingOptions.Value);

                flightPlan.SetCommands(currentPlanCommands);

                foreach (var cmd in currentPlanCommands)
                {
                    if (cmd.ExecutionTime.HasValue)
                    {
                        if (cmd.ExecutionTime.Value <= selectedOverpass.EndTime)
                        {
                            return (false, $"Chronology Error: Command '{cmd.CommandType}' scheduled for {cmd.ExecutionTime.Value:O} occurs before or during the upload overpass (Ends: {selectedOverpass.EndTime:O}). Time travel is not supported.");
                        }
                    }
                }

                var activePlans = await repository.GetActivePlansBySatelliteAsync(flightPlan.SatelliteId);

                var conflictMargin = TimeSpan.FromMinutes(2);

                foreach (var activePlan in activePlans)
                {
                    if (activePlan.Id == flightPlan.Id) continue;

                    var activeCommands = activePlan.GetCommands();

                    foreach (var newCmd in currentPlanCommands)
                    {
                        if (!newCmd.ExecutionTime.HasValue) continue;

                        foreach (var existingCmd in activeCommands)
                        {
                            if (!existingCmd.ExecutionTime.HasValue) continue;

                            var timeDiff = Math.Abs((newCmd.ExecutionTime.Value - existingCmd.ExecutionTime.Value).TotalSeconds);

                            if (timeDiff < conflictMargin.TotalSeconds)
                            {
                                return (false, $"Conflict Error: This plan conflicts with active Flight Plan #{activePlan.Id} ('{activePlan.Name}'). " +
                                               $"Command execution times overlap at {newCmd.ExecutionTime.Value:O} (Margin: {conflictMargin.TotalMinutes} min).");
                            }
                        }
                    }
                }

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
            var flightPlan = await repository.GetByIdAsync(flightPlanId);
            if (flightPlan == null) throw new ArgumentException($"Flight plan with ID {flightPlanId} not found.");

            var satellite = await satelliteService.GetAsync(flightPlan.SatelliteId);
            if (satellite == null) throw new ArgumentException($"Satellite with ID {flightPlan.SatelliteId} not found.");

            var commands = flightPlan.GetCommands();
            var (isValid, errors) = commands.ValidateAll();
            if (!isValid) throw new InvalidOperationException($"Cannot compile invalid flight plan. Errors: {string.Join("; ", errors)}");

            try
            {
                await commands.CalculateExecutionTimesAsync(satellite, imagingCalculation, imagingOptions.Value);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Failed to calculate execution times: {ex.Message}", ex);
            }

            return await commands.CompileAllToCsh();
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