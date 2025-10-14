using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using SatOps.Data;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Overpass;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.Gateway;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.TLE;
using SGPdotNET.Util;

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
    }

    public class FlightPlanService(
        IFlightPlanRepository repository,
        SatOpsDbContext dbContext,
        ISatelliteService satelliteService,
        IGroundStationService groundStationService,
        IOverpassService overpassService,
        IImagingCalculation imagingCalculation,
        IGroundStationGatewayService gatewayService,
        ILogger<IFlightPlanService> logger
        ) : IFlightPlanService
    {
        private readonly IFlightPlanRepository _repository;
        private readonly IGroundStationService _groundStationService;
        private readonly ISatelliteService _satelliteService;
        private readonly IService _overpassService;

        public FlightPlanService(
            IFlightPlanRepository repository,
            IGroundStationService groundStationService,
            ISatelliteService satelliteService,
            IService overpassService,
            IImagingCalculation imagingCalculation
        )
        {
            _repository = repository;
            _groundStationService = groundStationService;
            _satelliteService = satelliteService;
            _overpassService = overpassService;
            _imagingCalculation = imagingCalculation;
        }

        public Task<List<FlightPlan>> ListAsync() => _repository.GetAllAsync();

        public Task<FlightPlan?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<FlightPlan> CreateAsync(CreateFlightPlanDto createDto)
        {
            // Validate that the groundstation exists
            var groundStation = await _groundStationService.GetAsync(createDto.GsId);

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

            var commandSequence = CommandSequence.FromJsonElement(createDto.Commands);
            var (isValid, errors) = commandSequence.ValidateAll();
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
                CreatedById = 1, // TODO: Extract from token
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            entity.SetCommandSequence(commandSequence);
            return await _repository.AddAsync(entity);
        }

        public async Task<FlightPlan?> CreateNewVersionAsync(int id, CreateFlightPlanDto updateDto)
        {
            var existing = await _repository.GetByIdAsync(id);
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
            await _repository.UpdateAsync(existing);

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

            return await _repository.AddAsync(newVersion);
        }

        public async Task<(bool Success, string Message)> ApproveOrRejectAsync(int id, string status)
        {
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
                var commandSequence = plan.GetCommandSequence();
                var (isValid, errors) = commandSequence.ValidateAll();
                if (!isValid)
                {
                    return (false, $"Cannot approve invalid flight plan: {string.Join("; ", errors)}");
                }
            }

            plan.Status = newStatus;

            plan.ApprovalDate = DateTime.UtcNow;
            plan.ApprovedById = 1; // TODO: Extract from token

            await _repository.UpdateAsync(plan);

            return (true, $"Flight plan {status.ToLowerInvariant()} successfully");
        }

        public async Task<(bool Success, string Message)> AssociateWithOverpassAsync(
            int id,
            AssociateOverpassDto dto)
        {
            try
            {
                var flightPlan = await _repository.GetByIdAsync(id);
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

                var availableOverpasses = await _overpassService.CalculateOverpassesAsync(overpassCalculationRequest);
                if (availableOverpasses?.Count == 0)
                {
                    return (false, "No matching overpass found");
                }

                var selectedOverpass = availableOverpasses?.FirstOrDefault();
                if (selectedOverpass == null)
                {
                    return (false, "No suitable overpass found");
                }

                var storedOverpass = await _overpassService.FindOrCreateOverpassAsync(selectedOverpass);
                if (storedOverpass == null)
                {
                    return (false, "Failed to store overpass data");
                }

                flightPlan.OverpassId = storedOverpass.Id;
                flightPlan.ScheduledAt = selectedOverpass.StartTime;
                flightPlan.Status = FlightPlanStatus.AssignedToOverpass;
                flightPlan.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(flightPlan);

                return (true, "Flight plan associated with overpass successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error associating flight plan with overpass: {ex.Message}");
            }
        }

        public async Task<List<string>> CompileFlightPlanToCshAsync(int flightPlanId)
        {
            var flightPlan = await _repository.GetByIdAsync(flightPlanId);
            if (flightPlan == null)
            {
                throw new ArgumentException($"Flight plan with ID {flightPlanId} not found.");
            }

            var commandSequence = flightPlan.GetCommandSequence();

            // Validate before compiling
            var (isValid, errors) = commandSequence.ValidateAll();
            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"Cannot compile invalid flight plan. Errors: {string.Join("; ", errors)}");
            }

            return await commandSequence.CompileAllToCsh();
        }
        
                public async Task<ImagingTimingResponseDto> GetImagingOpportunity(ImagingTimingRequestDto request)
        {
            var satellite = await _satelliteService.GetAsync(request.SatelliteId);
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

            var imagingOpportunity = _imagingCalculation.FindBestImagingOpportunity(
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
    }
}