using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SatOps.Modules.User;

namespace SatOps.Modules.FlightPlan
{
    public interface IFlightPlanService
    {
        Task<List<FlightPlan>> ListAsync();
        Task<FlightPlan?> GetByIdAsync(int id);
        Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto);
        Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto);
        Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status);
        Task<(bool Success, string Message)> AssociateWithOverpassAsync(int flightPlanId, AssociateOverpassDto overpassRequest);
        Task<List<string>> CompileFlightPlanToCshAsync(int id);
        Task<ImagingTimingResponseDto> GetImagingOpportunity(ImagingTimingRequestDto request);
        Task UpdateFlightPlanStatusAsync(int flightPlanId, FlightPlanStatus newStatus, string? failureReason = null);
        Task<List<FlightPlan>> GetPlansReadyForTransmissionAsync(TimeSpan lookahead);
    }

    public class FlightPlanService(
        IFlightPlanRepository repository,
        ISatelliteService satelliteService,
        IGroundStationService groundStationService,
        IOverpassService overpassService,
        IImagingCalculation imagingCalculation,
        ICurrentUserProvider currentUserProvider
    ) : IFlightPlanService
    {
        public Task<List<FlightPlan>> ListAsync() => repository.GetAllAsync();

        public Task<FlightPlan?> GetByIdAsync(int id) => repository.GetByIdReadOnlyAsync(id);

        // In Modules/FlightPlan/Service.cs

        public async Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto)
        {
            var currentUserId = currentUserProvider.GetUserId()
                                ?? throw new UnauthorizedAccessException("User not authenticated.");

            // Validate that the groundstation exists
            var groundStation = await groundStationService.GetAsync(createDto.GsId);
            if (groundStation == null)
            {
                throw new ArgumentException($"Ground station with ID {createDto.GsId} not found.", nameof(createDto.GsId));
            }

            // Validate that the satellite exists
            var satellite = await satelliteService.GetAsync(createDto.SatId);
            if (satellite == null)
            {
                throw new ArgumentException($"Satellite with ID {createDto.SatId} not found.", nameof(createDto.SatId));
            }

            // Validate commands
            var (isValid, errors) = createDto.Commands.ValidateAll();
            if (!isValid)
            {
                throw new ArgumentException(
                    $"Command validation failed: {string.Join("; ", errors)}");
            }

            // Create entity
            var entity = new FlightPlan
            {
                Name = createDto.Name,
                GroundStationId = createDto.GsId,
                SatelliteId = createDto.SatId,
                Status = FlightPlanStatus.Draft,
                CreatedById = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            entity.SetCommands(createDto.Commands);
            return await repository.AddAsync(entity);
        }

        public async Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto)
        {
            var existing = await repository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            // Only certain statuses can be updated
            if (existing.Status != FlightPlanStatus.Draft &&
                existing.Status != FlightPlanStatus.Approved &&
                existing.Status != FlightPlanStatus.AssignedToOverpass)
            {
                return null;
            }

            // Wrap commands in CommandSequence for validation
            /*    var commandSequence = new CommandSequence { Commands = updateDto.Commands };
               var (isValid, errors) = commandSequence.ValidateAll();
               if (!isValid)
               {
                   throw new ArgumentException(
                       $"Command validation failed: {string.Join("; ", errors)}");
               } */

            // Mark the old plan as superseded
            existing.Status = FlightPlanStatus.Superseded;
            existing.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(existing);

            // Create new version
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

            //newVersion.SetCommandSequence(commandSequence);

            return await repository.AddAsync(newVersion);
        }

        public async Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status)
        {
            var currentUserId = currentUserProvider.GetUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");

            var plan = await repository.GetByIdAsync(id);
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

            var newStatus = FlightPlanStatusExtensions.FromScreamCase(status);

            if (newStatus == FlightPlanStatus.Approved)
            {
                // Re-validate before approval
                var commands = plan.GetCommands();
                var (isValid, errors) = commands.ValidateAll();
                if (!isValid)
                {
                    return (false, $"Cannot approve invalid flight plan: {string.Join("; ", errors)}");
                }
            }

            plan.Status = newStatus;

            plan.ApprovalDate = DateTime.UtcNow;
            plan.ApprovedById = currentUserId;

            await repository.UpdateAsync(plan);
            return (true, $"Flight plan {status.ToLowerInvariant()} successfully");
        }

        public async Task<(bool Success, string Message)> AssociateWithOverpassAsync(
            int id,
            AssociateOverpassDto dto)
        {
            try
            {
                var flightPlan = await repository.GetByIdAsync(id);
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
                var expandedStartTime = dto.StartTime.AddHours(-toleranceHours);
                var expandedEndTime = dto.EndTime.AddHours(toleranceHours);

                // Create overpass calculation request
                var overpassCalculationRequest = new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = flightPlan.SatelliteId,
                    GroundStationId = flightPlan.GroundStationId,
                    StartTime = expandedStartTime,
                    EndTime = expandedEndTime,
                };

                var availableOverpasses = await overpassService.CalculateOverpassesAsync(overpassCalculationRequest);
                if (availableOverpasses?.Count == 0)
                {
                    return (false, "No matching overpass found");
                }

                var selectedOverpass = availableOverpasses?.FirstOrDefault();
                if (selectedOverpass == null)
                {
                    return (false, "No suitable overpass found");
                }

                var storedOverpass = await overpassService.FindOrCreateOverpassAsync(selectedOverpass);
                if (storedOverpass == null)
                {
                    return (false, "Failed to store overpass data");
                }

                flightPlan.OverpassId = storedOverpass.Id;
                flightPlan.ScheduledAt = selectedOverpass.StartTime;
                flightPlan.Status = FlightPlanStatus.AssignedToOverpass;
                flightPlan.UpdatedAt = DateTime.UtcNow;

                await repository.UpdateAsync(flightPlan);

                return (true, "Flight plan associated with overpass successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error associating flight plan with overpass: {ex.Message}");
            }
        }

        public async Task<List<string>> CompileFlightPlanToCshAsync(int flightPlanId)
        {
            var flightPlan = await repository.GetByIdAsync(flightPlanId);
            if (flightPlan == null)
            {
                throw new ArgumentException($"Flight plan with ID {flightPlanId} not found.");
            }

            var commands = flightPlan.GetCommands();

            // Validate before compiling
            var (isValid, errors) = commands.ValidateAll();
            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"Cannot compile invalid flight plan. Errors: {string.Join("; ", errors)}");
            }

            return await commands.CompileAllToCsh();
        }

        public async Task<ImagingTimingResponseDto> GetImagingOpportunity(ImagingTimingRequestDto request)
        {
            var satellite = await satelliteService.GetAsync(request.SatelliteId);
            if (satellite == null)
            {
                return new ImagingTimingResponseDto
                {
                    Message = $"Satellite with ID {request.SatelliteId} not found."
                };
            }

            if (string.IsNullOrWhiteSpace(satellite.TleLine1) || string.IsNullOrWhiteSpace(satellite.TleLine2))
            {
                return new ImagingTimingResponseDto
                {
                    Message = $"Satellite with ID {request.SatelliteId} does not have valid TLE data."
                };
            }

            var tle = new Tle(satellite.Name, satellite.TleLine1, satellite.TleLine2);
            var sgp4Satellite = new SGPdotNET.Observation.Satellite(tle);

            // Check TLE age and warn if > 48 hours
            var tleAge = DateTime.UtcNow - tle.Epoch;
            var tleAgeWarning = tleAge.TotalHours > 48;

            // Create target coordinate
            var targetCoordinate = new GeodeticCoordinate(
                Angle.FromDegrees(request.TargetLatitude),
                Angle.FromDegrees(request.TargetLongitude),
                0); // Assuming ground level target

            var maxSearchDuration = TimeSpan.FromHours(request.MaxSearchDurationHours);

            var imagingOpportunity = imagingCalculation.FindBestImagingOpportunity(
                sgp4Satellite,
                targetCoordinate,
                request.CommandReceptionTime ?? DateTime.UtcNow,
                maxSearchDuration
            );

            var result = new ImagingTimingResponseDto
            {
                ImagingTime = imagingOpportunity.ImagingTime,
                OffNadirDegrees = imagingOpportunity.OffNadirDegrees,
                SatelliteAltitudeKm = imagingOpportunity.SatelliteAltitudeKm,
                TleAgeWarning = tleAgeWarning,
                TleAgeHours = tleAge.TotalHours,
            };

            if (result.OffNadirDegrees > request.MaxOffNadirDegrees)
            {
                result.Message = $"No imaging opportunity found within the off-nadir limit of {request.MaxOffNadirDegrees} degrees. Best one found was {result.OffNadirDegrees:F2} degrees off-nadir.";
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