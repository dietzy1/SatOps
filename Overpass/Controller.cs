using Microsoft.AspNetCore.Mvc;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.Observation;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using SGPdotNET.Util;


// GET Endpoint that retrieves overpasses within a time window for a given ground station and satellite provided in the path
// GET Endpoint that retrieves the next overpass window for a given ground station and satellite provided in the path


namespace SatOps.Overpass
{
    [ApiController]
    [Route("api/v1/overpass")]
    public class Controller : ControllerBase
    {
        private readonly IService _overpassService;

        public Controller(IService overpassService)
        {
            _overpassService = overpassService;
        }

        // GET Endpoint that retrieves overpasses within a time window for a given ground station and satellite provided in the path
        [HttpGet("windows/satellite/{satelliteId:int}/groundstation/{groundStationId:int}")]
        public async Task<ActionResult<List<OverpassWindowDto>>> GetOverpassWindows(
            int satelliteId,
            int groundStationId,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] double minimumElevation = 0.0)
        {
            var startTime = request.StartTime ?? DateTime.UtcNow;
            var endTime = request.EndTime ?? startTime.AddDays(7);
            // Validate time window
            if (endTime <= startTime)
            {
                throw new Exception("End time must be after start time.");
            }

            var minimumElevation = request.MinimumElevation;
            if (minimumElevation < 0 || minimumElevation > 90)
            {
                throw new Exception($"Minimum elevation of {minimumElevation} must be between 0 and 90 degrees.");
            }
            try
            {
                var result = await _overpassService.CalculateOverpassesAsync(new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = satelliteId,
                    GroundStationId = groundStationId,
                    StartTime = startTime,
                    EndTime = endTime,
                    MinimumElevation = minimumElevation
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return InternalServerError($"{ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error calculating overpass windows: {ex.Message}");
            }
        }

        // GET Endpoint that retrieves the next overpass window for a given ground station and satellite provided in the path
        [HttpGet("next/{groundStationId:int}")]
        public async Task<ActionResult<OverpassWindowDto>> GetNextOverpass(
            int satelliteId,
            int groundStationId,
            [FromQuery] DateTime? fromTime = null,
            [FromQuery] double minimumElevation = 0.0)
        {
            try
            {
                // Get satellite data
                var satellite = await _satelliteService.GetAsync(satelliteId);
                if (satellite == null)
                {
                    return NotFound($"Satellite with ID {satelliteId} not found.");
                }

                // Get ground station data
                var groundStationEntity = await _groundStationService.GetAsync(groundStationId);
                if (groundStationEntity == null)
                {
                    return NotFound($"Ground station with ID {groundStationId} not found.");
                }

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                {
                    return BadRequest("Satellite TLE data is not available.");
                }

                // Use provided time or current UTC time
                var searchStartTime = fromTime ?? DateTime.UtcNow;
                var searchEndTime = searchStartTime.AddDays(7); // Search up to 7 days ahead

                // Create TLE strings
                var tle1 = satellite.Name;
                var tle2 = satellite.TleLine1;
                var tle3 = satellite.TleLine2;

                // Create a TLE and then satellite from the TLEs
                var tle = new Tle(tle1, tle2, tle3);
                var sat = new SGPdotNET.Observation.Satellite(tle);

                // Set up ground station location from stored coordinates
                var latitude = groundStationEntity.Location.Y;
                var longitude = groundStationEntity.Location.X;
                var location = new GeodeticCoordinate(
                    Angle.FromDegrees(latitude),
                    Angle.FromDegrees(longitude),
                    0.0); // Assuming sea level for stored ground stations

                // Create a ground station
                var groundStation = new SGPdotNET.Observation.GroundStation(location);

                var timeStep = TimeSpan.FromMinutes(1); // Check every minute
                var currentTime = searchStartTime;
                var inOverpass = false;
                var overpassStart = DateTime.MinValue;
                var maxElevation = 0.0;
                var maxElevationTime = DateTime.MinValue;
                var startAzimuth = 0.0;

                while (currentTime <= searchEndTime)
                {
                    var observation = groundStation.Observe(sat, currentTime);
                    var elevation = observation.Elevation.Degrees;
                    var azimuth = observation.Azimuth.Degrees;

                    if (!inOverpass && elevation > minimumElevation)
                    {
                        // Starting an overpass
                        inOverpass = true;
                        overpassStart = currentTime;
                        maxElevation = elevation;
                        maxElevationTime = currentTime;
                        startAzimuth = azimuth;
                    }
                    else if (inOverpass && elevation > minimumElevation)
                    {
                        // Continuing overpass, check if this is the maximum elevation
                        if (elevation > maxElevation)
                        {
                            maxElevation = elevation;
                            maxElevationTime = currentTime;
                        }
                    }
                    else if (inOverpass && elevation <= minimumElevation)
                    {
                        // Ending an overpass - return the first one found
                        var duration = (currentTime - overpassStart).TotalSeconds;

                        var nextOverpass = new OverpassWindowDto
                        {
                            SatelliteId = satelliteId,
                            SatelliteName = satellite.Name,
                            GroundStationId = groundStationId,
                            GroundStationName = groundStationEntity.Name,
                            StartTime = overpassStart,
                            EndTime = currentTime,
                            MaxElevationTime = maxElevationTime,
                            MaxElevation = maxElevation,
                            Duration = duration,
                            StartAzimuth = startAzimuth,
                            EndAzimuth = azimuth
                        };

                        return Ok(nextOverpass);
                    }

                    currentTime = currentTime.Add(timeStep);
                }

                return NotFound("No overpass found within the search window (7 days).");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error calculating next overpass: {ex.Message}");
            }
        }

    }
}