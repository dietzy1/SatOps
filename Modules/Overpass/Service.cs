using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.Observation;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.Overpass
{
    public interface IService
    {
        Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request);
        Task<Entity> StoreOverpassAsync(OverpassWindowDto overpassWindow);
        Task<Entity?> GetStoredOverpassAsync(int id);
        Task<Entity?> FindOrCreateOverpassAsync(OverpassWindowDto overpassWindow);
    }

    public class Service : IService
    {
        private readonly ISatelliteService _satelliteService;
        private readonly IGroundStationService _groundStationService;
        private readonly IOverpassRepository _overpassRepository;

        public Service(ISatelliteService satelliteService, IGroundStationService groundStationService, IOverpassRepository overpassRepository)
        {
            _satelliteService = satelliteService;
            _groundStationService = groundStationService;
            _overpassRepository = overpassRepository;
        }

        public async Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request)
        {
            try
            {
                // Get satellite data
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
                        var durationSeconds = (int)(currentTime - overpassStart).TotalSeconds;

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

                return overpassWindows;
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

        public async Task<Entity> StoreOverpassAsync(OverpassWindowDto overpassWindow)
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
                EndAzimuth = overpassWindow.EndAzimuth
            };

            return await _overpassRepository.AddAsync(overpassEntity);
        }

        public async Task<Entity?> GetStoredOverpassAsync(int id)
        {
            return await _overpassRepository.GetByIdReadOnlyAsync(id);
        }

        public async Task<Entity?> FindOrCreateOverpassAsync(OverpassWindowDto overpassWindow)
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
            return await StoreOverpassAsync(overpassWindow);
        }
    }
}