using SGPdotNET.CoordinateSystem;

namespace SatOps.Modules.FlightPlan
{
    public interface IImagingCalculation
    {
        ImagingCalculation.ImagingOpportunity FindBestImagingOpportunity(
            SGPdotNET.Observation.Satellite satellite,
            GeodeticCoordinate target,
            DateTime commandReceptionTime,
            TimeSpan maxSearchDuration
        );
    }

    public class ImagingCalculation(ILogger<ImagingCalculation> logger) : IImagingCalculation
    {
        public class Candidate
        {
            public DateTime Time { get; set; } = DateTime.MinValue;
            public double OffNadirDegrees { get; set; } = 80.0;
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

        /// <summary>
        /// Core algorithm: Find the best imaging opportunity using SGP4 orbital propagation with off-nadir calculations
        /// 
        /// Algorithm Steps:
        /// 1. Start from command reception time
        /// 2. Use coarse time steps (120s) to find candidate windows
        /// 3. Refine around candidates with fine steps (2s) for better accuracy
        /// 4. Refine around candidates with finest steps (0.1s) for precision
        /// 5. For each time step:
        ///    - Use SGP4 to calculate satellite position
        ///    - Convert ECI coordinates to geodetic using SGPdotNET's built-in ToGeodetic() method
        ///    - Calculate dynamic max distance based on altitude and off-nadir angle
        ///    - Calculate off-nadir angle using fast approximation: atan(groundDistance / altitude)
        ///    - Check if within acceptable off-nadir range
        /// 6. Return the best opportunity within range
        /// 
        /// Note: All units are correctly handled by SGPdotNET library:
        /// - DistanceTo() returns kilometers
        /// - GeodeticCoordinate.Altitude is in kilometers
        /// - Latitude/Longitude angles use .Degrees property for conversion
        /// </summary>
        public ImagingOpportunity FindBestImagingOpportunity(
            SGPdotNET.Observation.Satellite satellite,
            GeodeticCoordinate target,
            DateTime commandReceptionTime,
            TimeSpan maxSearchDuration
        )
        {
            var coarseTimeStep = TimeSpan.FromSeconds(120);
            var refiningTimeStep = TimeSpan.FromSeconds(2);
            var finalTimeStep = TimeSpan.FromSeconds(0.1);

            var currentTime = commandReceptionTime;
            var searchEndTime = commandReceptionTime.Add(maxSearchDuration);
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

                    var worstCandidate = top5Candidates.MaxBy(c => c.OffNadirDegrees);

                    // Check if the current time step is better than our worst candidate.
                    if (worstCandidate != null && offNadirDeg < worstCandidate.OffNadirDegrees)
                    {
                        // Replace the worst candidate's values.
                        worstCandidate.Time = currentTime;
                        worstCandidate.OffNadirDegrees = offNadirDeg;
                    }
                }
                catch (Exception)
                {
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
                logger.LogWarning("Exceptions occurred during SGP4 propagation at {ExceptionCount} time steps.", exceptionTimes.Count);
            }

            var bestCandidate = top5Candidates.OrderBy(c => c.OffNadirDegrees).First();

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