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

            PassPoint Evaluate(DateTime t)
            {
                var satEci = satellite.Predict(t);


                var targetEci = target.ToEci(t);

                // Create Range Vector (Target Position - Sat Position)
                var rangeVector = targetEci.Position - satEci.Position;

                var slantRangeKm = rangeVector.Length;

                // Calculate Off-Nadir Angle
                // Use Dot Product formula: A . B = |A| * |B| * cos(angle)

                // Vector A: Satellite Position (Vector from Earth Center -> Satellite)
                // Vector B: Range Vector (Vector from Satellite -> Target)
                var satPosVector = satEci.Position;

                // Calculate the Dot Product
                var dot = satPosVector.Dot(rangeVector);

                // Calculate magnitudes
                var magSat = satPosVector.Length;
                var magRange = rangeVector.Length; // same as slantRangeKm

                // Calculate Cosine of the angle
                // "UP" is SatPos and "DOWN" is Range.
                var cosTheta = dot / (magSat * magRange);

                // Clamp for floating point safety
                if (cosTheta > 1.0) cosTheta = 1.0;
                if (cosTheta < -1.0) cosTheta = -1.0;

                // Acos gives radians. 
                // Since the vectors point in opposite general directions,
                // The Off-Nadir angle is PI (180 deg) minus the calculated angle.
                var angleRadians = Math.Acos(cosTheta);
                var offNadirRadians = Math.PI - angleRadians;
                var offNadirDeg = offNadirRadians * (180.0 / Math.PI);

                // Horizon Check
                if (!target.CanSee(satEci))
                {
                    return PassPoint.Empty;
                }

                return new PassPoint(t, offNadirDeg, slantRangeKm, satEci.ToGeodetic());
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