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
using SatOps.Services.GroundStation;
using SatOps.Services.Satellite;

namespace SatOps.Overpass
{
    public interface IService
    {
        Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request);
    }

    public class Service : IService
    {
        private readonly ISatelliteService _satelliteService;
        private readonly IGroundStationService _groundStationService;

        public Service(ISatelliteService satelliteService, IGroundStationService groundStationService)
        {
            _satelliteService = satelliteService;
            _groundStationService = groundStationService;
        }

        public async Task<List<OverpassWindowDto>> CalculateOverpassesAsync(OverpassWindowsCalculationRequestDto request)
        {
            try
            {
                // Get satellite data
                var satellite = await _satelliteService.GetAsync(request.SatelliteId);
                if (satellite == null)
                {
                    throw new Exception($"Satellite with ID {request.SatelliteId} not found.");
                }

                // Get ground station data
                var groundStationEntity = await _groundStationService.GetAsync(request.GroundStationId);
                if (groundStationEntity == null)
                {
                    throw new Exception($"Ground station with ID {request.GroundStationId} not found.");
                }

                if (string.IsNullOrEmpty(satellite.TleLine1) || string.IsNullOrEmpty(satellite.TleLine2))
                {
                    throw new Exception("Satellite TLE data is not available.");
                }

                

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
                var overpassWindows = new List<OverpassWindowDto>();
                var timeStep = TimeSpan.FromMinutes(1); // Check every minute
                var currentTime = startTime;
                var inOverpass = false;
                var overpassStart = DateTime.MinValue;
                var maxElevation = 0.0;
                var maxElevationTime = DateTime.MinValue;
                var startAzimuth = 0.0;

                while (currentTime <= endTime)
                {
                    var observation = groundStation.Observe(sat, startTime);
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
                        // Ending an overpass
                        inOverpass = false;
                        var duration = (currentTime - overpassStart).TotalSeconds;

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
                            Duration = duration,
                            StartAzimuth = startAzimuth,
                            EndAzimuth = azimuth
                        });
                    }

                    currentTime = currentTime.Add(timeStep);
                }

                return overpassWindows;
            }
            catch
            {
                throw new InvalidOperationException("Internal server error");
            }
        }
    }
}