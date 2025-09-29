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

        // Satellite position information
        public double? SatelliteLatitude { get; set; }
        public double? SatelliteLongitude { get; set; }
        public double? SatelliteAltitudeKm { get; set; }

        // TLE age warning
        public bool TleAgeWarning { get; set; }
        public double? TleAgeHours { get; set; }

        public string? Message { get; set; }
    }

    public class ImagingOpportunity
    {
        public DateTime ImagingTime { get; set; }
        public double DistanceKm { get; set; }
        public double OffNadirDegrees { get; set; }
        public double SlantRangeKm { get; set; }
        public double SatelliteLatitude { get; set; }
        public double SatelliteLongitude { get; set; }
        public double SatelliteAltitudeKm { get; set; }
    }

    public class Candidate
    {
        public DateTime Time { get; set; } = DateTime.MinValue;
        public double OffNadirDegrees { get; set; } = 80.0;
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

                if (bestOpportunity != null)
                {
                    var sleepDuration = bestOpportunity.ImagingTime - request.CommandReceptionTime;

                    return Ok(new ImagingTimingResponseDto
                    {
                        ImagingOpportunityFound = true,
                        ImagingTime = bestOpportunity.ImagingTime,
                        SleepDuration = sleepDuration,
                        DistanceToTargetKm = bestOpportunity.DistanceKm,
                        OffNadirDegrees = bestOpportunity.OffNadirDegrees,
                        SlantRangeKm = bestOpportunity.SlantRangeKm,
                        SatelliteLatitude = bestOpportunity.SatelliteLatitude,
                        SatelliteLongitude = bestOpportunity.SatelliteLongitude,
                        SatelliteAltitudeKm = bestOpportunity.SatelliteAltitudeKm,
                        TleAgeWarning = tleAgeWarning,
                        TleAgeHours = tleAge.TotalHours,
                        Message = $"Imaging opportunity found. Satellite should sleep for {sleepDuration.TotalSeconds:F0} seconds " +
                                 $"(approximately {sleepDuration.TotalMinutes:F1} minutes) before imaging. " +
                                 $"Off-nadir angle: {bestOpportunity.OffNadirDegrees:F1}°" +
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
        private ImagingOpportunity? FindBestImagingOpportunity(SGPdotNET.Observation.Satellite satellite, GeodeticCoordinate target, ImagingTimingRequestDto request)
        {
            var coarseTimeStep = TimeSpan.FromSeconds(120);
            var refiningTimeStep = TimeSpan.FromSeconds(2);
            var finalTimeStep = TimeSpan.FromSeconds(0.1);

            var currentTime = request.CommandReceptionTime;
            var searchEndTime = request.CommandReceptionTime.Add(request.MaxSearchDuration);
            DateTime bestTime = DateTime.Now;
            var position = satellite.Predict(currentTime);

            // Step 1: Coarse search to find candidate windows
            var top5Candidates = new List<Candidate>();
            for (int i = 0; i < 5; i++)
            {
                top5Candidates.Add(new Candidate());
            }

            // Create array of exception times for logging/debugging if needed
            var exceptionTimes = new List<DateTime>();

            while (currentTime <= searchEndTime)
            {
                try
                {
                    // Get satellite position at current time using SGP4
                    position = satellite.Predict(currentTime);

                    var offNadirDeg = OffNadirDegrees(target, position, currentTime);

                    var maxOffNadir = top5Candidates.Max(c => c.OffNadirDegrees);

                    // Check if this is within our acceptable off-nadir range
                    if (offNadirDeg <= maxOffNadir)
                    {
                        int idx = top5Candidates.FindIndex(c => c.OffNadirDegrees == maxOffNadir);
                        if (idx >= 0)
                        {
                            top5Candidates[idx] = new Candidate
                            {
                                Time = currentTime,
                                OffNadirDegrees = offNadirDeg
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException("Index for max off-nadir candidate not found, this should never happen.");
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip this time step if calculation fails (e.g., satellite decay, invalid time)
                    // This can happen with old TLE data or satellites that have re-entered
                    exceptionTimes.Add(currentTime);
                }

                currentTime = currentTime.Add(coarseTimeStep);
            }

            // Step 2: Refinement around best candidates
            for (int i = 0; i < top5Candidates.Count; i++)
            {
                var bestOffNadir = top5Candidates[i].OffNadirDegrees;
                var refineStart = top5Candidates[i].Time.Subtract(coarseTimeStep);
                var refineEnd = top5Candidates[i].Time.Add(coarseTimeStep);
                currentTime = refineStart;
                while (currentTime <= refineEnd)
                {
                    try
                    {
                        position = satellite.Predict(currentTime);

                        var offNadirDeg = OffNadirDegrees(target, position, currentTime);

                        if (offNadirDeg < bestOffNadir)
                        {
                            bestTime = currentTime;
                            bestOffNadir = offNadirDeg;
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this time step if calculation fails
                        exceptionTimes.Add(currentTime);
                    }

                    currentTime = currentTime.Add(refiningTimeStep);
                }

                top5Candidates[i].Time = bestTime;
                top5Candidates[i].OffNadirDegrees = bestOffNadir;
            }

            // Step 3: Final refinement around best time found
            for (int i = 0; i < top5Candidates.Count; i++)
            {
                var bestOffNadir = top5Candidates[i].OffNadirDegrees;
                var refineStart = top5Candidates[i].Time.Subtract(refiningTimeStep);
                var refineEnd = top5Candidates[i].Time.Add(refiningTimeStep);
                currentTime = refineStart;
                while (currentTime <= refineEnd)
                {
                    try
                    {
                        position = satellite.Predict(currentTime);

                        var offNadirDeg = OffNadirDegrees(target, position, currentTime);

                        if (offNadirDeg < bestOffNadir)
                        {
                            bestTime = currentTime;
                            bestOffNadir = offNadirDeg;
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this time step if calculation fails
                        exceptionTimes.Add(currentTime);
                    }

                    currentTime = currentTime.Add(finalTimeStep);
                }

                top5Candidates[i].Time = bestTime;
                top5Candidates[i].OffNadirDegrees = bestOffNadir;
            }

            // Log the exception times if needed for debugging
            if (exceptionTimes.Count > 0)
            {
                _logger.LogWarning("Exceptions occurred during SGP4 propagation at {ExceptionCount} time steps.", exceptionTimes.Count);
            }

            var bestCandidate = top5Candidates.OrderBy(c => c.OffNadirDegrees).First();
            if (bestCandidate.OffNadirDegrees > request.MaxOffNadirDegrees)
            {
                // No suitable opportunity found within off-nadir constraints
                return null;
            }

            position = satellite.Predict(bestCandidate.Time);
            var eciCoordinate = new EciCoordinate(bestCandidate.Time, position.Position, position.Velocity);
            var geodetic = eciCoordinate.ToGeodetic();
            var satelliteCoordinate = new GeodeticCoordinate(
                geodetic.Latitude,
                geodetic.Longitude,
                geodetic.Altitude);

            var groundDistanceKm = target.DistanceTo(satelliteCoordinate);
            var slantRangeKm = Math.Sqrt(groundDistanceKm * groundDistanceKm + geodetic.Altitude * geodetic.Altitude);

            return new ImagingOpportunity
            {
                ImagingTime = bestCandidate.Time,
                DistanceKm = groundDistanceKm,
                OffNadirDegrees = bestCandidate.OffNadirDegrees,
                SlantRangeKm = slantRangeKm,
                SatelliteLatitude = geodetic.Latitude.Degrees,
                SatelliteLongitude = geodetic.Longitude.Degrees,
                SatelliteAltitudeKm = geodetic.Altitude
            };
        }

        private double OffNadirDegrees(GeodeticCoordinate target, EciCoordinate satellite, DateTime currentTime)
        {
            // Create EciCoordinate from the prediction result and convert to geodetic using SGPdotNET's built-in method
            var eciCoordinate = new EciCoordinate(currentTime, satellite.Position, satellite.Velocity);
            var geodetic = eciCoordinate.ToGeodetic();

            // Calculate distance between satellite ground track and target using SGPdotNET's built-in method
            var satelliteCoordinate = new GeodeticCoordinate(
                geodetic.Latitude,
                geodetic.Longitude,
                geodetic.Altitude);
            var groundDistanceKm = target.DistanceTo(satelliteCoordinate);

            var offNadirRad = Math.Atan(groundDistanceKm / geodetic.Altitude);

            return offNadirRad * 180.0 / Math.PI;
        }
    }
}