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
                // 1. Load Stored Data
                var storedOverpasses = await overpassRepository.FindStoredOverpassesInTimeRange(
                    request.SatelliteId,
                    request.GroundStationId,
                    request.StartTime,
                    request.EndTime);

                var satellite = await satelliteService.GetAsync(request.SatelliteId);
                if (satellite == null) throw new ArgumentException($"Satellite {request.SatelliteId} not found.");

                var groundStationEntity = await groundStationService.GetAsync(request.GroundStationId);
                if (groundStationEntity == null) throw new ArgumentException($"Ground station {request.GroundStationId} not found.");

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                    throw new InvalidOperationException("Satellite TLE data is not available.");

                // 2. Initialize SGP4
                var tle = new Tle(satellite.Name, satellite.TleLine1, satellite.TleLine2);
                var sat = new SGPdotNET.Observation.Satellite(tle);
                var location = new GeodeticCoordinate(
                    Angle.FromDegrees(groundStationEntity.Location.Latitude),
                    Angle.FromDegrees(groundStationEntity.Location.Longitude),
                    groundStationEntity.Location.Altitude);
                var groundStation = new SGPdotNET.Observation.GroundStation(location);

                var overpassWindows = new List<OverpassWindowDto>();

                // 3. Define Time Steps
                var coarseStep = TimeSpan.FromSeconds(60);
                var fineStep = TimeSpan.FromSeconds(1);    // For detecting AOS/LOS edges
                var precisionStep = TimeSpan.FromSeconds(0.1); // For detecting exact Max Elevation

                // Local function to get observation data at a specific time
                (double Elevation, double Azimuth) GetObservation(DateTime t)
                {
                    var obs = groundStation.Observe(sat, t);
                    return (obs.Elevation.Degrees, obs.Azimuth.Degrees);
                }

                // Local function: Given a coarse interval where we crossed the threshold, find the exact moment
                DateTime RefineBoundaryTime(DateTime startWindow, DateTime endWindow, double threshold, bool rising)
                {
                    var t = startWindow;
                    while (t <= endWindow)
                    {
                        var (ele, _) = GetObservation(t);
                        // If rising (AOS), we want first point >= threshold
                        // If falling (LOS), we want first point < threshold (so the previous second was the end)
                        if (rising && ele >= threshold) return t;
                        if (!rising && ele < threshold) return t.Subtract(fineStep);

                        t = t.Add(fineStep);
                    }
                    return rising ? endWindow : startWindow;
                }

                // Local function: Given a rough peak time, find the exact peak using steps
                (DateTime Time, double Elevation, double Azimuth) RefinePeak(DateTime roughPeakTime)
                {
                    // Scan +/- 90 seconds around rough peak with 1s step
                    var bestTime = roughPeakTime;
                    var (maxEle, bestAz) = GetObservation(roughPeakTime);

                    var startSearch = roughPeakTime.AddSeconds(-90);
                    var endSearch = roughPeakTime.AddSeconds(90);

                    // Pass 1: Fine Step (1s)
                    for (var t = startSearch; t <= endSearch; t = t.Add(fineStep))
                    {
                        var (ele, az) = GetObservation(t);
                        if (ele > maxEle)
                        {
                            maxEle = ele;
                            bestTime = t;
                            bestAz = az;
                        }
                    }

                    // Pass 2: Precision Step (0.1s) - scan +/- 2 seconds around the new best
                    var startPrecise = bestTime.AddSeconds(-2);
                    var endPrecise = bestTime.AddSeconds(2);

                    for (var t = startPrecise; t <= endPrecise; t = t.Add(precisionStep))
                    {
                        var (ele, az) = GetObservation(t);
                        if (ele > maxEle)
                        {
                            maxEle = ele;
                            bestTime = t;
                            bestAz = az;
                        }
                    }

                    return (bestTime, maxEle, bestAz);
                }

                // 4. Main Calculation Loop
                var currentTime = request.StartTime;

                // Track state
                bool isInsidePass = false;
                DateTime potentialAosStart = DateTime.MinValue;
                DateTime roughMaxTime = DateTime.MinValue;
                double roughMaxEle = -999.0;
                double startAzimuth = 0;

                // Pre-calculate previous point to detect transitions
                var (prevEle, _) = GetObservation(currentTime);

                while (currentTime <= request.EndTime)
                {
                    var nextTime = currentTime.Add(coarseStep);
                    var (currEle, currAz) = GetObservation(nextTime);

                    // --- Check for AOS (Rising Edge) ---
                    // Scenario: We were below limit, now we are above (or equal)
                    if (!isInsidePass && currEle >= request.MinimumElevation)
                    {
                        // We crossed the threshold somewhere between currentTime and nextTime.
                        // 1. Refine the exact start time
                        var preciseStart = RefineBoundaryTime(currentTime, nextTime, request.MinimumElevation, true);
                        var (_, preciseStartAz) = GetObservation(preciseStart);

                        isInsidePass = true;
                        potentialAosStart = preciseStart;
                        startAzimuth = preciseStartAz;

                        // Initialize rough max
                        roughMaxEle = currEle;
                        roughMaxTime = nextTime;
                    }
                    // --- While Inside Pass ---
                    else if (isInsidePass)
                    {
                        // Track rough max
                        if (currEle > roughMaxEle)
                        {
                            roughMaxEle = currEle;
                            roughMaxTime = nextTime;
                        }

                        // --- Check for LOS (Falling Edge) ---
                        // Scenario: We dropped below limit (or reached end of search window)
                        bool fallingEdge = currEle < request.MinimumElevation;
                        bool endOfWindow = nextTime >= request.EndTime;

                        if (fallingEdge || endOfWindow)
                        {
                            // 2. Refine the exact end time
                            // If it's a falling edge, the boundary is between current and next.
                            // If it's end of window, we just clamp to nextTime.
                            var preciseEnd = fallingEdge
                                ? RefineBoundaryTime(currentTime, nextTime, request.MinimumElevation, false)
                                : nextTime;

                            var (_, preciseEndAz) = GetObservation(preciseEnd);

                            // 3. Calculate Duration
                            var durationSeconds = (preciseEnd - potentialAosStart).TotalSeconds;

                            // 4. Validate Duration
                            if (!request.MinimumDurationSeconds.HasValue || durationSeconds >= request.MinimumDurationSeconds.Value)
                            {
                                // 5. Find Exact Peak (TCA)
                                // We use the roughMaxTime found during the loop as the seed
                                var (preciseMaxTime, preciseMaxEle, _) = RefinePeak(roughMaxTime);

                                overpassWindows.Add(new OverpassWindowDto
                                {
                                    SatelliteId = satellite.Id,
                                    SatelliteName = satellite.Name,
                                    GroundStationId = groundStationEntity.Id,
                                    GroundStationName = groundStationEntity.Name,
                                    StartTime = potentialAosStart,
                                    EndTime = preciseEnd,
                                    MaxElevationTime = preciseMaxTime,
                                    MaxElevation = preciseMaxEle,
                                    DurationSeconds = durationSeconds,
                                    StartAzimuth = startAzimuth,
                                    EndAzimuth = preciseEndAz
                                });
                            }

                            // Reset state
                            isInsidePass = false;
                            roughMaxEle = -999.0;
                            roughMaxTime = DateTime.MinValue;

                            if (request.MaxResults.HasValue && overpassWindows.Count >= request.MaxResults.Value)
                            {
                                break;
                            }
                        }
                    }

                    prevEle = currEle;
                    currentTime = nextTime;
                }

                return await MergeAndEnrichOverpasses(overpassWindows, storedOverpasses);
            }
            catch (ArgumentException) { throw; }
            catch (InvalidOperationException) { throw; }
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
                    $"in this time window.");
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
            foreach (var calculatedOverpass in result)
            {
                var toleranceMinutes = 10;
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
            return result.OrderBy(o => o.StartTime).ToList();
        }
    }
}