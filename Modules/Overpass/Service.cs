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
        Task<Entity?> GetStoredOverpassAsync(int id);
        Task<(bool Success, Entity? Overpass, string Message)> FindOrCreateOverpassForFlightPlanAsync(
            OverpassWindowDto overpassWindow,
            int flightPlanId,
            int toleranceMinutes,
            string? tleLine1 = null,
            string? tleLine2 = null,
            DateTime? tleUpdateTime = null);
    }

    public class OverpassService(ISatelliteService satelliteService, IGroundStationService groundStationService, IOverpassRepository overpassRepository) : IOverpassService
    {
        public async Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request)
        {
            try
            {
                // First, check if we have stored overpasses in the requested time range
                var storedOverpasses = await overpassRepository.FindStoredOverpassesInTimeRange(
                    request.SatelliteId,
                    request.GroundStationId,
                    request.StartTime,
                    request.EndTime);

                // Get satellite data for calculations and names
                var satellite = await satelliteService.GetAsync(request.SatelliteId);
                if (satellite == null)
                {
                    throw new ArgumentException($"Satellite with ID {request.SatelliteId} not found.");
                }

                // Get ground station data
                var groundStationEntity = await groundStationService.GetAsync(request.GroundStationId);
                if (groundStationEntity == null)
                {
                    throw new ArgumentException($"Ground station with ID {request.GroundStationId} not found.");
                }

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                {
                    throw new InvalidOperationException("Satellite TLE data is not available.");
                }

                var tle1 = satellite.Name;
                var tle2 = satellite.TleLine1;
                var tle3 = satellite.TleLine2;


                var tle = new Tle(tle1, tle2, tle3);
                var sat = new SGPdotNET.Observation.Satellite(tle);

                var location = new GeodeticCoordinate(
                    Angle.FromDegrees(groundStationEntity.Location.Latitude),
                    Angle.FromDegrees(groundStationEntity.Location.Longitude),
                    groundStationEntity.Location.Altitude); // Assuming sea level for stored ground stations

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

                    if (!inOverpass && elevation >= request.MinimumElevation)
                    {
                        // Starting an overpass
                        inOverpass = true;
                        overpassStart = currentTime;
                        maxElevation = elevation;
                        maxElevationTime = currentTime;
                        startAzimuth = azimuth;
                    }
                    else if (inOverpass && elevation >= request.MinimumElevation)
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
                var mergedOverpasses = await MergeAndEnrichOverpasses(overpassWindows, storedOverpasses);

                return mergedOverpasses;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw ArgumentException to be handled by the controller
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

        public async Task<Entity?> GetStoredOverpassAsync(int id)
        {
            return await overpassRepository.GetByIdReadOnlyAsync(id);
        }

        public async Task<(bool Success, Entity? Overpass, string Message)> FindOrCreateOverpassForFlightPlanAsync(
            OverpassWindowDto overpassWindow,
            int flightPlanId,
            int toleranceMinutes,
            string? tleLine1 = null,
            string? tleLine2 = null,
            DateTime? tleUpdateTime = null)
        {
            // Check if there's already an overpass in this time window
            var existingOverpass = await overpassRepository.FindOverpassInTimeWindowAsync(
                overpassWindow.SatelliteId,
                overpassWindow.GroundStationId,
                overpassWindow.StartTime,
                overpassWindow.EndTime,
                toleranceMinutes
            );

            if (existingOverpass != null)
            {
                return (false, null,
                    $"An overpass is already assigned to flight plan '{existingOverpass.FlightPlan?.Name ?? "Unknown"}' (ID: {existingOverpass.FlightPlanId}) " +
                    $"in this time window. Each satellite pass can only be assigned to one flight plan.");
            }

            var overpassEntity = new Entity
            {
                SatelliteId = overpassWindow.SatelliteId,
                GroundStationId = overpassWindow.GroundStationId,
                FlightPlanId = flightPlanId,
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

            var createdOverpass = await overpassRepository.AddAsync(overpassEntity);
            return (true, createdOverpass, "Overpass created and assigned successfully.");
        }

        private async Task<List<OverpassWindowDto>> MergeAndEnrichOverpasses(
            List<OverpassWindowDto> calculatedOverpasses,
            List<Entity> storedOverpasses)
        {
            var result = calculatedOverpasses;

            // For each calculated overpass, check if we already have a stored one
            foreach (var calculatedOverpass in result)
            {
                var toleranceMinutes = 10; // Allow 10-minute tolerance for merging

                // Check if this calculated overpass is already in the database
                var storedOverpass = storedOverpasses.FirstOrDefault(co =>
                    co.SatelliteId == calculatedOverpass.SatelliteId &&
                    co.GroundStationId == calculatedOverpass.GroundStationId &&
                    Math.Abs((co.StartTime - calculatedOverpass.StartTime).TotalMinutes) < toleranceMinutes &&
                    Math.Abs((co.EndTime - calculatedOverpass.EndTime).TotalMinutes) < toleranceMinutes);

                if (storedOverpass != null)
                {
                    var flightPlan = await overpassRepository.GetAssociatedFlightPlanAsync(storedOverpass.Id);
                    if (flightPlan != null)
                    {
                        calculatedOverpass.AssociatedFlightPlan = new AssociatedFlightPlanDto
                        {
                            Id = flightPlan.Id,
                            Name = flightPlan.Name,
                            ScheduledAt = flightPlan.ScheduledAt,
                            Status = flightPlan.Status.ToString(),
                            ApproverId = flightPlan.ApprovedById?.ToString(),
                            ApprovalDate = flightPlan.ApprovalDate
                        };
                    }
                }
            }

            // Sort by start time
            return result.OrderBy(o => o.StartTime).ToList();
        }
    }
}