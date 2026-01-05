using SGPdotNET.CoordinateSystem;
using SGPdotNET.Propagation;

namespace SatOps.Modules.FlightPlan
{
    public interface IImagingCalculation
    {
        ImagingCalculation.ImagingOpportunity? FindBestImagingOpportunity(
            SGPdotNET.Observation.Satellite satellite,
            GeodeticCoordinate target,
            DateTime commandReceptionTime,
            TimeSpan maxSearchDuration
        );
    }

    public class ImagingCalculation(ILogger<ImagingCalculation> logger) : IImagingCalculation
    {
        private readonly struct PassPoint
        {
            public DateTime Time { get; }
            public double OffNadirDegrees { get; }
            public double SlantRangeKm { get; }
            public GeodeticCoordinate? SatelliteGeo { get; }

            public PassPoint(DateTime time, double offNadir, double slantRange, GeodeticCoordinate? geo)
            {
                Time = time;
                OffNadirDegrees = offNadir;
                SlantRangeKm = slantRange;
                SatelliteGeo = geo;
            }

            public static PassPoint Empty => new(DateTime.MinValue, double.MaxValue, 0, null);
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
        /// Find the best imaging opportunity using SGP4 orbital propagation.
        /// Returns null if no valid opportunity is found within maxSearchDuration.
        /// </summary>
        public ImagingOpportunity? FindBestImagingOpportunity(
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

            var top5Candidates = new PassPoint[5];
            Array.Fill(top5Candidates, PassPoint.Empty);

            var exceptionCount = 0;

            // Local function to evaluate a single point in time
            PassPoint Evaluate(DateTime t)
            {
                try
                {
                    var eci = satellite.Predict(t);
                    var satGeo = eci.ToGeodetic();

                    // --- 1. Geometric Setup ---
                    // We model the Earth Center (C), Target (T), and Satellite (S) as a triangle.
                    // Re = Radius of Earth
                    // Rs = Radius of Satellite orbit (Re + Altitude)
                    // Theta (θ) = Central angle between Target and Satellite Nadir
                    double earthRadius = SgpConstants.EarthRadiusKm;
                    double satRadius = earthRadius + satGeo.Altitude;

                    // AngleTo returns the angular distance between two points on the sphere
                    var theta = target.AngleTo(satGeo).Radians;
                    var cosTheta = Math.Cos(theta);

                    // --- 2. Horizon Check ---
                    // The target must be visible from the satellite (Elevation > 0).
                    // The geometric limit is when the line of sight is tangent to the Earth's surface.
                    // This occurs when cos(theta_max) = Re / Rs.
                    // If theta > theta_max (or cos(theta) < Re/Rs), the target is over the horizon.
                    if (cosTheta < earthRadius / satRadius)
                    {
                        return PassPoint.Empty;
                    }

                    // --- 3. Slant Range Calculation (Law of Cosines) ---
                    // Solves for side 'c' (Slant Range) in triangle CTS:
                    // c² = a² + b² - 2ab * cos(θ)
                    var slantRangeSq = (earthRadius * earthRadius) + (satRadius * satRadius)
                                       - (2 * earthRadius * satRadius * cosTheta);
                    var slantRange = Math.Sqrt(slantRangeSq);

                    // --- 4. Off-Nadir Angle Calculation (Law of Sines) ---
                    // Solves for angle 'S' (Off-Nadir) in triangle CTS:
                    // a / sin(A) = c / sin(C)  =>  Re / sin(OffNadir) = SlantRange / sin(Theta)
                    var sinOffNadir = earthRadius * Math.Sin(theta) / slantRange;

                    // Clamp value to [-1, 1] range to handle potential floating point 
                    // epsilon errors before calling Arcsin.
                    if (sinOffNadir > 1.0) sinOffNadir = 1.0;
                    if (sinOffNadir < -1.0) sinOffNadir = -1.0;

                    var offNadirDeg = Math.Asin(sinOffNadir) * (180.0 / Math.PI);

                    return new PassPoint(t, offNadirDeg, slantRange, satGeo);
                }
                catch (Exception)
                {
                    exceptionCount++;
                    return PassPoint.Empty;
                }
            }

            // Step 1: Coarse Search (120s step)
            while (currentTime <= searchEndTime)
            {
                var currentPoint = Evaluate(currentTime);
                if (currentPoint.Time != DateTime.MinValue)
                {
                    // Basic eviction policy: Keep the top 5 lowest Off-Nadir angles
                    int worstIndex = -1;
                    double worstOffNadir = double.MinValue;
                    for (int i = 0; i < top5Candidates.Length; i++)
                    {
                        if (top5Candidates[i].OffNadirDegrees > worstOffNadir)
                        {
                            worstOffNadir = top5Candidates[i].OffNadirDegrees;
                            worstIndex = i;
                        }
                    }
                    if (worstIndex != -1 && currentPoint.OffNadirDegrees < worstOffNadir)
                        top5Candidates[worstIndex] = currentPoint;
                }
                currentTime = currentTime.Add(coarseTimeStep);
            }

            // Step 2: Refinement (2s step)
            for (int i = 0; i < top5Candidates.Length; i++)
            {
                if (top5Candidates[i].Time == DateTime.MinValue) continue;
                var bestLocal = top5Candidates[i];
                var start = bestLocal.Time.Subtract(coarseTimeStep);
                var end = bestLocal.Time.Add(coarseTimeStep);
                var t = start;
                while (t <= end)
                {
                    var p = Evaluate(t);
                    if (p.Time != DateTime.MinValue && p.OffNadirDegrees < bestLocal.OffNadirDegrees)
                        bestLocal = p;
                    t = t.Add(refiningTimeStep);
                }
                top5Candidates[i] = bestLocal;
            }

            // Step 3: Final Refinement (0.1s step)
            for (int i = 0; i < top5Candidates.Length; i++)
            {
                if (top5Candidates[i].Time == DateTime.MinValue) continue;
                var bestLocal = top5Candidates[i];
                var start = bestLocal.Time.Subtract(refiningTimeStep);
                var end = bestLocal.Time.Add(refiningTimeStep);
                var t = start;
                while (t <= end)
                {
                    var p = Evaluate(t);
                    if (p.Time != DateTime.MinValue && p.OffNadirDegrees < bestLocal.OffNadirDegrees)
                        bestLocal = p;
                    t = t.Add(finalTimeStep);
                }
                top5Candidates[i] = bestLocal;
            }

            if (exceptionCount > 0)
                logger.LogWarning("Exceptions occurred during SGP4 propagation at {ExceptionCount} steps.", exceptionCount);

            // Select absolute best
            var bestCandidate = top5Candidates
                .Where(c => c.Time != DateTime.MinValue)
                .OrderBy(c => c.OffNadirDegrees)
                .FirstOrDefault();

            var finalGeo = bestCandidate.SatelliteGeo;
            if (bestCandidate.Time == DateTime.MinValue || finalGeo == null)
            {
                return null;
            }

            // Arc length = Radius * Angle(radians)
            var groundDistanceKm = SgpConstants.EarthRadiusKm * target.AngleTo(finalGeo).Radians;

            return new ImagingOpportunity
            {
                ImagingTime = bestCandidate.Time,
                DistanceKm = groundDistanceKm,
                OffNadirDegrees = bestCandidate.OffNadirDegrees,
                SlantRangeKm = bestCandidate.SlantRangeKm,
                SatelliteLatitude = finalGeo.Latitude.Degrees,
                SatelliteLongitude = finalGeo.Longitude.Degrees,
                SatelliteAltitudeKm = finalGeo.Altitude
            };
        }
    }
}