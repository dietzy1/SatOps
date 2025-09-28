using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using SGPdotNET.Util;

namespace SatOps.Modules.Overpass
{
    // DTO for the imaging timing request
    public class ImagingTimingRequestDto
    {
        public int SatelliteId { get; set; }
        public DateTime CommandReceptionTime { get; set; }
        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }

        // Off-nadir imaging parameters (replaces fixed MaxDistanceKm)
        public double MaxOffNadirDegrees { get; set; } = 10.0; // Default 10° off-nadir maximum

        // Search parameters
        public TimeSpan MaxSearchDuration { get; set; } = TimeSpan.FromHours(48); // Search up to 48 hours ahead
        public TimeSpan CoarseTimeStepSize { get; set; } = TimeSpan.FromSeconds(30); // 30-second coarse steps
        public TimeSpan RefinementWindow { get; set; } = TimeSpan.FromSeconds(120); // ±120s refinement window
        public TimeSpan RefinementStepSize { get; set; } = TimeSpan.FromSeconds(1); // 1-second refinement steps
    }

    // DTO for the response
    public class ImagingTimingResponseDto
    {
        public bool ImagingOpportunityFound { get; set; }
        public DateTime? ImagingTime { get; set; }
        public TimeSpan? SleepDuration { get; set; }

        // Geometry information for flight software decision making
        public double? DistanceToTargetKm { get; set; }
        public double? OffNadirDegrees { get; set; }
        public double? SlantRangeKm { get; set; }
        public double? MaxAllowedDistanceKm { get; set; } // Dynamic distance based on altitude and off-nadir

        // Satellite position information
        public double? SatelliteLatitude { get; set; }
        public double? SatelliteLongitude { get; set; }
        public double? SatelliteAltitudeKm { get; set; }

        // TLE age warning
        public bool TleAgeWarning { get; set; }
        public double? TleAgeHours { get; set; }

        public string? Message { get; set; }
    }

    /// <summary>
    /// Test Controller for Satellite Imaging Timing Calculations with Off-Nadir Angle Support
    /// 
    /// Core Problem Solved: Given a ground station communication window and a target coordinate,
    /// calculate how long a satellite should sleep before being positioned over the target for imaging,
    /// using realistic off-nadir angle constraints instead of fixed distance thresholds.
    /// 
    /// Key Improvements:
    /// - Uses off-nadir angle (0° = nadir/straight down) with configurable max (default 10°)
    /// - Dynamic distance calculation: MaxDistanceKm = altitudeKm * tan(maxOffNadirRad)
    /// - Fast approximation: offNadir ≈ atan(groundDistance / altitude)
    /// - Coarse → refine time search for efficiency and precision
    /// - TLE age warnings (>48 hours)
    /// - Returns geometry info for flight software decisions
    /// 
    /// High-Level Solution:
    /// 1. Receive satellite ID, command reception time, and target coordinates
    /// 2. Use SGP4 orbital propagation to predict satellite positions over time
    /// 3. Find when satellite will be within acceptable off-nadir angle of target
    /// 4. Calculate sleep duration = imaging_time - command_reception_time
    /// 5. Return timing and geometry information to satellite for mission execution
    /// </summary>
    [ApiController]
    [Route("api/v1/test/imaging-timing")]
    public class TestImagingTimingController : ControllerBase
    {
        private readonly Satellite.ISatelliteService _satelliteService;
        private readonly ILogger<TestImagingTimingController> _logger;

        public TestImagingTimingController(Satellite.ISatelliteService satelliteService, ILogger<TestImagingTimingController> logger)
        {
            _satelliteService = satelliteService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate when a satellite should perform imaging based on target coordinates
        /// 
        /// This endpoint solves the core orbital mechanics problem:
        /// - Takes a satellite ID, command time, and target location
        /// - Uses SGP4 propagation to predict satellite ground track
        /// - Finds when satellite will be over target coordinates
        /// - Returns sleep duration for satellite mission timing
        /// </summary>
        [HttpPost("calculate")]
        public async Task<ActionResult<ImagingTimingResponseDto>> CalculateImagingTiming([FromBody] ImagingTimingRequestDto request)
        {
            try
            {
                // Validate request parameters
                if (request.SatelliteId <= 0)
                    return BadRequest("Invalid satellite ID");

                if (request.TargetLatitude < -90 || request.TargetLatitude > 90)
                    return BadRequest("Target latitude must be between -90 and 90 degrees");

                if (request.TargetLongitude < -180 || request.TargetLongitude > 180)
                    return BadRequest("Target longitude must be between -180 and 180 degrees");

                if (request.MaxOffNadirDegrees <= 0 || request.MaxOffNadirDegrees > 90)
                    return BadRequest("Max off-nadir angle must be between 0 and 90 degrees");

                // Get satellite data
                var satellite = await _satelliteService.GetAsync(request.SatelliteId);
                if (satellite == null)
                {
                    return NotFound(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = false,
                        Message = $"Satellite with ID {request.SatelliteId} not found."
                    });
                }

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                {
                    return BadRequest(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = false,
                        Message = "Satellite TLE data is not available."
                    });
                }

                // Mock TLE data temporarily for testing with northern satelites
                // TODO: We should be seeding the database with these coordinates
                // Core issue is that satelittes with our approach cant image all locations on earth
                // So if we want to take a picture at high latitudes(places to the north) we need a satellite with high inclination
                satellite.Name = "SENTINEL-2C";
                satellite.TleLine1 = "1 60989U 24157A   25270.79510520  .00000303  00000-0  13232-3 0  9996";
                satellite.TleLine2 = "2 60989  98.5675 344.4033 0001006  86.9003 273.2295 14.30815465 55465";


                // Create TLE and satellite objects for SGP4 calculations
                var tle = new Tle(satellite.Name, satellite.TleLine1, satellite.TleLine2);
                var sgp4Satellite = new SGPdotNET.Observation.Satellite(tle);

                // Check TLE age and warn if > 48 hours
                var tleAge = DateTime.UtcNow - tle.Epoch;
                var tleAgeWarning = tleAge.TotalHours > 48;

                double inclinationDeg = tle.Inclination.Degrees; // property name may vary by lib - check API
                double targetLatAbs = Math.Abs(request.TargetLatitude);

                if (targetLatAbs > inclinationDeg)
                {
                    _logger.LogWarning("Target latitude {TargetLatitude}° exceeds satellite inclination {Inclination}°. Imaging not possible.", request.TargetLatitude, inclinationDeg);
                    return Ok(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = false,
                        TleAgeWarning = tleAgeWarning,
                        TleAgeHours = tleAge.TotalHours,
                        Message = $"Target latitude {request.TargetLatitude}° exceeds satellite inclination {inclinationDeg:F2}°. " +
                                  "With a nadir (body-fixed) camera the satellite cannot overfly that latitude." +
                                  (tleAgeWarning ? $" WARNING: TLE data is {tleAge.TotalHours:F0} hours old!" : "")
                    });
                }

                // Create target coordinate
                var targetCoordinate = new GeodeticCoordinate(
                    Angle.FromDegrees(request.TargetLatitude),
                    Angle.FromDegrees(request.TargetLongitude),
                    0); // Assuming ground level target

                // Search for imaging opportunity using SGP4 propagation with off-nadir calculations
                var bestOpportunity = FindBestImagingOpportunity(sgp4Satellite, targetCoordinate, request);

                if (bestOpportunity.HasValue)
                {
                    var sleepDuration = bestOpportunity.Value.ImagingTime - request.CommandReceptionTime;

                    return Ok(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = true,
                        ImagingTime = bestOpportunity.Value.ImagingTime,
                        SleepDuration = sleepDuration,
                        DistanceToTargetKm = bestOpportunity.Value.DistanceKm,
                        OffNadirDegrees = bestOpportunity.Value.OffNadirDegrees,
                        SlantRangeKm = bestOpportunity.Value.SlantRangeKm,
                        MaxAllowedDistanceKm = bestOpportunity.Value.MaxAllowedDistanceKm,
                        SatelliteLatitude = bestOpportunity.Value.SatelliteLatitude,
                        SatelliteLongitude = bestOpportunity.Value.SatelliteLongitude,
                        SatelliteAltitudeKm = bestOpportunity.Value.SatelliteAltitudeKm,
                        TleAgeWarning = tleAgeWarning,
                        TleAgeHours = tleAge.TotalHours,
                        Message = $"Imaging opportunity found. Satellite should sleep for {sleepDuration.TotalSeconds:F0} seconds " +
                                 $"(approximately {sleepDuration.TotalMinutes:F1} minutes) before imaging. " +
                                 $"Off-nadir angle: {bestOpportunity.Value.OffNadirDegrees:F1}°" +
                                 (tleAgeWarning ? $" WARNING: TLE data is {tleAge.TotalHours:F0} hours old!" : "")
                    });
                }
                else
                {
                    _logger.LogInformation("No imaging opportunity found within {MaxSearchDuration} hours and {MaxOffNadirDegrees}° off-nadir for target coordinates ({TargetLatitude}, {TargetLongitude})",
                        request.MaxSearchDuration.TotalHours, request.MaxOffNadirDegrees, request.TargetLatitude, request.TargetLongitude);
                    return Ok(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = false,
                        TleAgeWarning = tleAgeWarning,
                        TleAgeHours = tleAge.TotalHours,
                        Message = $"No imaging opportunity found within {request.MaxSearchDuration.TotalHours} hours " +
                                 $"and {request.MaxOffNadirDegrees}° off-nadir for target coordinates " +
                                 $"({request.TargetLatitude:F4}, {request.TargetLongitude:F4})." +
                                 (tleAgeWarning ? $" WARNING: TLE data is {tleAge.TotalHours:F0} hours old!" : "")
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ImagingTimingResponseDto
                {
                    ImagingOpportunityFound = false,
                    Message = $"Error calculating imaging timing: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Core algorithm: Find the best imaging opportunity using SGP4 orbital propagation with off-nadir calculations
        /// 
        /// Algorithm Steps:
        /// 1. Start from command reception time
        /// 2. Use coarse time steps (30s) to find candidate windows
        /// 3. Refine around candidates with fine steps (1s) for precision
        /// 4. For each time step:
        ///    - Use SGP4 to calculate satellite position
        ///    - Convert ECI coordinates to geodetic using SGPdotNET's built-in ToGeodetic() method
        ///    - Calculate dynamic max distance based on altitude and off-nadir angle
        ///    - Calculate off-nadir angle using fast approximation: atan(groundDistance / altitude)
        ///    - Check if within acceptable off-nadir range
        /// 5. Return the best opportunity within range
        /// 
        /// Note: All units are correctly handled by SGPdotNET library:
        /// - DistanceTo() returns kilometers
        /// - GeodeticCoordinate.Altitude is in kilometers
        /// - Latitude/Longitude angles use .Degrees property for conversion
        /// </summary>
        private (DateTime ImagingTime, double DistanceKm, double OffNadirDegrees, double SlantRangeKm, double MaxAllowedDistanceKm, double SatelliteLatitude, double SatelliteLongitude, double SatelliteAltitudeKm)?
            FindBestImagingOpportunity(SGPdotNET.Observation.Satellite satellite, GeodeticCoordinate target, ImagingTimingRequestDto request)
        {
            // Convert max off-nadir to radians for calculations
            var maxOffNadirRad = request.MaxOffNadirDegrees * Math.PI / 180.0;

            var currentTime = request.CommandReceptionTime;
            var searchEndTime = request.CommandReceptionTime.Add(request.MaxSearchDuration);
            (DateTime ImagingTime, double DistanceKm, double OffNadirDegrees, double SlantRangeKm, double MaxAllowedDistanceKm, double SatelliteLatitude, double SatelliteLongitude, double SatelliteAltitudeKm)? bestOpportunity = null;
            double bestOffNadir = double.MaxValue;

            // Step 1: Coarse search to find candidate windows
            var candidates = new List<DateTime>();

            // Create array of exception times for logging/debugging if needed
            var exceptionTimes = new List<DateTime>();

            while (currentTime <= searchEndTime)
            {
                try
                {
                    // Get satellite position at current time using SGP4
                    var position = satellite.Predict(currentTime);

                    // Create EciCoordinate from the prediction result and convert to geodetic using SGPdotNET's built-in method
                    var eciCoordinate = new EciCoordinate(currentTime, position.Position, position.Velocity);
                    var geodetic = eciCoordinate.ToGeodetic();

                    // Calculate distance between satellite ground track and target using SGPdotNET's built-in method
                    var satelliteCoordinate = new GeodeticCoordinate(
                        geodetic.Latitude,
                        geodetic.Longitude,
                        geodetic.Altitude);
                    var groundDistanceKm = target.DistanceTo(satelliteCoordinate);

                    // Calculate dynamic max allowed distance based on altitude and off-nadir angle
                    var altitudeKm = geodetic.Altitude; // Already in kilometers
                    var maxAllowedDistanceKm = altitudeKm * Math.Tan(maxOffNadirRad);

                    var offNadirRad = Math.Atan(groundDistanceKm / altitudeKm);
                    var offNadirDeg = offNadirRad * 180.0 / Math.PI;

                    // Check if this is within our acceptable off-nadir range
                    if (offNadirDeg <= request.MaxOffNadirDegrees)
                    {
                        candidates.Add(currentTime);

                        // Track the best opportunity during coarse search
                        if (offNadirDeg < bestOffNadir)
                        {
                            // Calculate slant range (3D distance from satellite to target)
                            // Using target at ground level (0 altitude) vs satellite at altitude
                            var targetAtGround = new GeodeticCoordinate(target.Latitude, target.Longitude, 0);
                            var slantRangeKm = satelliteCoordinate.DistanceTo(targetAtGround);

                            bestOffNadir = offNadirDeg;
                            bestOpportunity = (
                                ImagingTime: currentTime,
                                DistanceKm: groundDistanceKm,
                                OffNadirDegrees: offNadirDeg,
                                SlantRangeKm: slantRangeKm,
                                MaxAllowedDistanceKm: maxAllowedDistanceKm,
                                SatelliteLatitude: geodetic.Latitude.Degrees,
                                SatelliteLongitude: geodetic.Longitude.Degrees,
                                SatelliteAltitudeKm: altitudeKm
                            );
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip this time step if calculation fails (e.g., satellite decay, invalid time)
                    // This can happen with old TLE data or satellites that have re-entered
                    exceptionTimes.Add(currentTime);
                }

                currentTime = currentTime.Add(request.CoarseTimeStepSize);
            }

            // Step 2: Refinement around best candidate if we found any
            if (bestOpportunity.HasValue && candidates.Count > 0)
            {
                var candidateTime = bestOpportunity.Value.ImagingTime;
                var refineStart = candidateTime.Subtract(request.RefinementWindow);
                var refineEnd = candidateTime.Add(request.RefinementWindow);

                // Reset for refinement search
                bestOffNadir = double.MaxValue;
                var refinedOpportunity = bestOpportunity;

                currentTime = refineStart;
                while (currentTime <= refineEnd)
                {
                    try
                    {
                        var position = satellite.Predict(currentTime);
                        var eciCoordinate = new EciCoordinate(currentTime, position.Position, position.Velocity);
                        var geodetic = eciCoordinate.ToGeodetic();

                        var satelliteCoordinate = new GeodeticCoordinate(
                            geodetic.Latitude,
                            geodetic.Longitude,
                            geodetic.Altitude);
                        var groundDistance = target.DistanceTo(satelliteCoordinate);

                        var altitudeKm = geodetic.Altitude;
                        var maxAllowedDistanceKm = altitudeKm * Math.Tan(maxOffNadirRad);

                        var offNadirRad = Math.Atan(groundDistance / altitudeKm);
                        var offNadirDeg = offNadirRad * 180.0 / Math.PI;

                        if (offNadirDeg <= request.MaxOffNadirDegrees && offNadirDeg < bestOffNadir)
                        {
                            var targetAtGround = new GeodeticCoordinate(target.Latitude, target.Longitude, 0);
                            var slantRangeKm = satelliteCoordinate.DistanceTo(targetAtGround);

                            bestOffNadir = offNadirDeg;
                            refinedOpportunity = (
                                ImagingTime: currentTime,
                                DistanceKm: groundDistance,
                                OffNadirDegrees: offNadirDeg,
                                SlantRangeKm: slantRangeKm,
                                MaxAllowedDistanceKm: maxAllowedDistanceKm,
                                SatelliteLatitude: geodetic.Latitude.Degrees,
                                SatelliteLongitude: geodetic.Longitude.Degrees,
                                SatelliteAltitudeKm: altitudeKm
                            );
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this time step if calculation fails
                        exceptionTimes.Add(currentTime);
                    }

                    currentTime = currentTime.Add(request.RefinementStepSize);
                }

                bestOpportunity = refinedOpportunity;
            }

            // Log the exception times if needed for debugging
            if (exceptionTimes.Count > 0)
            {
                _logger.LogWarning("Exceptions occurred during SGP4 propagation at {ExceptionCount} time steps.", exceptionTimes.Count);
            }

            return bestOpportunity;
        }
    }
}