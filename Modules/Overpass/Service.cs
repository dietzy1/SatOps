using SGPdotNET.CoordinateSystem;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.Overpass
{
    public interface IOverpassService
    {
        Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request);
        Task<Entity> StoreOverpassAsync(OverpassWindowDto overpassWindow, string? tleLine1 = null, string? tleLine2 = null, DateTime? tleUpdateTime = null);
        Task<Entity?> GetStoredOverpassAsync(int id);
        Task<Entity?> FindOrCreateOverpassAsync(OverpassWindowDto overpassWindow, string? tleLine1 = null, string? tleLine2 = null, DateTime? tleUpdateTime = null);
    }

    public class OverpassService : IOverpassService
    {
        private readonly ISatelliteService _satelliteService;
        private readonly IGroundStationService _groundStationService;
        private readonly IOverpassRepository _overpassRepository;

        public OverpassService(ISatelliteService satelliteService, IGroundStationService groundStationService, IOverpassRepository overpassRepository)
        {
            _satelliteService = satelliteService;
            _groundStationService = groundStationService;
            _overpassRepository = overpassRepository;
        }

        public async Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request)
        {
            try
            {
                // First, check if we have stored overpasses in the requested time range
                var storedOverpasses = await _overpassRepository.FindStoredOverpassesInTimeRange(
                    request.SatelliteId,
                    request.GroundStationId,
                    request.StartTime,
                    request.EndTime);

                // Get satellite data for calculations and names
                var satellite = await _satelliteService.GetAsync(request.SatelliteId);
                if (satellite == null)
                {
                    throw new ArgumentException($"Satellite with ID {request.SatelliteId} not found.");
                }

                // Get ground station data
                var groundStationEntity = await _groundStationService.GetAsync(request.GroundStationId);
                if (groundStationEntity == null)
                {
                    throw new ArgumentException($"Ground station with ID {request.GroundStationId} not found.");
                }

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                {
                    throw new InvalidOperationException("Satellite TLE data is not available.");
                }

                // Create TLE strings
                var tle1 = satellite.Name;
                var tle2 = satellite.TleLine1;
                var tle3 = satellite.TleLine2;

                // Create a TLE and then satellite from the TLEs
                var tle = new Tle(tle1, tle2, tle3);
                var sat = new SGPdotNET.Observation.Satellite(tle);

                // Set up ground station location from stored coordinates
                var location = new GeodeticCoordinate(
                    Angle.FromDegrees(groundStationEntity.Location.Latitude),
                    Angle.FromDegrees(groundStationEntity.Location.Longitude),
                    groundStationEntity.Location.Altitude); // Assuming sea level for stored ground stations

                // Create a ground station
                var groundStation = new SGPdotNET.Observation.GroundStation(location);
                var overpassWindows = new List<OverpassWindowDto>();
                var timeStep = TimeSpan.FromMinutes(1); // Check every minute
                var currentTime = request.StartTime;
                var inOverpass = false;
                var overpassStart = DateTime.MinValue;
                var maxElevation = 0.0;
                var maxElevationTime = DateTime.MinValue;
                var startAzimuth = 0.0;

                while (currentTime <= request.EndTime)
                {
                    var observation = groundStation.Observe(sat, currentTime);
                    var elevation = observation.Elevation.Degrees;
                    var azimuth = observation.Azimuth.Degrees;

                    if (!inOverpass && elevation > request.MinimumElevation)
                    {
                        // Starting an overpass
                        inOverpass = true;
                        overpassStart = currentTime;
                        maxElevation = elevation;
                        maxElevationTime = currentTime;
                        startAzimuth = azimuth;
                    }
                    else if (inOverpass && elevation > request.MinimumElevation)
                    {
                        // Continuing overpass, check if this is the maximum elevation
                        if (elevation > maxElevation)
                        {
                            maxElevation = elevation;
                            maxElevationTime = currentTime;
                        }
                    }
                    else if (inOverpass && elevation <= request.MinimumElevation)
                    {
                        // Ending an overpass
                        inOverpass = false;
                        var durationSeconds = (int)(currentTime - overpassStart).TotalSeconds - 60; // Subtract the last minute where it went below minimum elevation

                        if (request.MinimumDurationSeconds.HasValue && durationSeconds < request.MinimumDurationSeconds.Value)
                        {
                            // Skip this overpass as it doesn't meet the minimum duration
                            currentTime = currentTime.Add(timeStep);
                            continue;
                        }

                        overpassWindows.Add(new OverpassWindowDto
                        {
                            SatelliteId = satellite.Id,
                            SatelliteName = satellite.Name,
                            GroundStationId = groundStationEntity.Id,
                            GroundStationName = groundStationEntity.Name,
                            StartTime = overpassStart,
                            EndTime = currentTime,
                            MaxElevationTime = maxElevationTime,
                            MaxElevation = maxElevation,
                            DurationSeconds = durationSeconds,
                            StartAzimuth = startAzimuth,
                            EndAzimuth = azimuth
                        });

                        // Check if we have reached the maximum number of results
                        if (request.MaxResults.HasValue && overpassWindows.Count >= request.MaxResults.Value)
                        {
                            break;
                        }
                    }

                    currentTime = currentTime.Add(timeStep);
                }

                // Merge calculated overpasses with stored overpasses and enrich with flight plan data
                var mergedOverpasses = await MergeAndEnrichOverpasses(overpassWindows, storedOverpasses, satellite.Name, groundStationEntity.Name);

                return mergedOverpasses;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw InvalidOperationException to be handled by the controller
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error calculating overpasses: {ex.Message}", ex);
            }
        }

        public async Task<Entity> StoreOverpassAsync(OverpassWindowDto overpassWindow, string? tleLine1 = null, string? tleLine2 = null, DateTime? tleUpdateTime = null)
        {
            var overpassEntity = new Entity
            {
                SatelliteId = overpassWindow.SatelliteId,
                GroundStationId = overpassWindow.GroundStationId,
                StartTime = overpassWindow.StartTime,
                EndTime = overpassWindow.EndTime,
                MaxElevationTime = overpassWindow.MaxElevationTime,
                MaxElevation = overpassWindow.MaxElevation,
                DurationSeconds = (int)overpassWindow.DurationSeconds,
                StartAzimuth = overpassWindow.StartAzimuth,
                EndAzimuth = overpassWindow.EndAzimuth,
                TleLine1 = tleLine1,
                TleLine2 = tleLine2,
                TleUpdateTime = tleUpdateTime
            };

            return await _overpassRepository.AddAsync(overpassEntity);
        }

        public async Task<Entity?> GetStoredOverpassAsync(int id)
        {
            return await _overpassRepository.GetByIdReadOnlyAsync(id);
        }

        public async Task<Entity?> FindOrCreateOverpassAsync(OverpassWindowDto overpassWindow, string? tleLine1 = null, string? tleLine2 = null, DateTime? tleUpdateTime = null)
        {
            // First try to find an existing overpass that matches
            var existingOverpass = await _overpassRepository.FindExistingOverpassAsync(
                overpassWindow.SatelliteId,
                overpassWindow.GroundStationId,
                overpassWindow.StartTime,
                overpassWindow.EndTime,
                overpassWindow.MaxElevation
            );

            if (existingOverpass != null)
            {
                return existingOverpass;
            }

            // If not found, create and store a new one
            return await StoreOverpassAsync(overpassWindow, tleLine1, tleLine2, tleUpdateTime);
        }

        private async Task<List<OverpassWindowDto>> MergeAndEnrichOverpasses(
            List<OverpassWindowDto> calculatedOverpasses,
            List<Entity> storedOverpasses,
            string satelliteName,
            string groundStationName)
        {
            var result = new List<OverpassWindowDto>();

            // Add calculated overpasses first
            result.AddRange(calculatedOverpasses);

            // For each stored overpass, check if we already have a similar calculated one
            foreach (var storedOverpass in storedOverpasses)
            {
                var toleranceMinutes = 10; // Allow 10-minute tolerance for merging

                // Check if this stored overpass is already represented in calculated overpasses
                var existingCalculated = result.FirstOrDefault(co =>
                    Math.Abs((co.StartTime - storedOverpass.StartTime).TotalMinutes) < toleranceMinutes &&
                    Math.Abs((co.EndTime - storedOverpass.EndTime).TotalMinutes) < toleranceMinutes);

                if (existingCalculated != null)
                {
                    // Enrich the existing calculated overpass with stored data
                    await EnrichOverpassWithStoredData(existingCalculated, storedOverpass);
                }
                else
                {
                    // Add stored overpass as new entry if it's not already calculated
                    var storedAsDto = await ConvertStoredOverpassToDto(storedOverpass, satelliteName, groundStationName);
                    result.Add(storedAsDto);
                }
            }

            // Sort by start time
            return result.OrderBy(o => o.StartTime).ToList();
        }

        private async Task EnrichOverpassWithStoredData(OverpassWindowDto overpassDto, Entity storedOverpass)
        {
            // Add TLE data if available
            if (!string.IsNullOrEmpty(storedOverpass.TleLine1) && !string.IsNullOrEmpty(storedOverpass.TleLine2))
            {
                overpassDto.TleData = new TleDataDto
                {
                    TleLine1 = storedOverpass.TleLine1,
                    TleLine2 = storedOverpass.TleLine2,
                    UpdateTime = storedOverpass.TleUpdateTime
                };
            }

            // Add associated flight plan if available
            var flightPlan = await _overpassRepository.GetAssociatedFlightPlanAsync(storedOverpass.Id);
            if (flightPlan != null)
            {
                overpassDto.AssociatedFlightPlan = new AssociatedFlightPlanDto
                {
                    Id = flightPlan.Id,
                    Name = flightPlan.Name,
                    ScheduledAt = flightPlan.ScheduledAt,
                    Status = flightPlan.Status.ToString(),
                    ApproverId = flightPlan.ApproverId,
                    ApprovalDate = flightPlan.ApprovalDate
                };
            }
        }

        private async Task<OverpassWindowDto> ConvertStoredOverpassToDto(Entity storedOverpass, string satelliteName, string groundStationName)
        {
            var dto = new OverpassWindowDto
            {
                SatelliteId = storedOverpass.SatelliteId,
                SatelliteName = satelliteName,
                GroundStationId = storedOverpass.GroundStationId,
                GroundStationName = groundStationName,
                StartTime = storedOverpass.StartTime,
                EndTime = storedOverpass.EndTime,
                MaxElevationTime = storedOverpass.MaxElevationTime,
                MaxElevation = storedOverpass.MaxElevation,
                DurationSeconds = storedOverpass.DurationSeconds,
                StartAzimuth = storedOverpass.StartAzimuth,
                EndAzimuth = storedOverpass.EndAzimuth
            };

            // Enrich with stored data
            await EnrichOverpassWithStoredData(dto, storedOverpass);

            return dto;
        }
    }
}