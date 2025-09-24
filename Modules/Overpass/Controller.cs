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
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] double minimumElevation = 0.0)
        {
            startTime ??= DateTime.UtcNow;
            endTime ??= startTime.Value.AddDays(7);
            // Validate time window
            if (endTime <= startTime)
            {
                throw new Exception("End time must be after start time.");
            }

            try
            {
                var result = await _overpassService.CalculateOverpassesAsync(new OverpassWindowsCalculationRequestDto
                {
                    SatelliteId = satelliteId,
                    GroundStationId = groundStationId,
                    StartTime = startTime.Value,
                    EndTime = endTime.Value,
                    MinimumElevation = minimumElevation
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, $"{ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }

        // GET Endpoint that retrieves the next overpass window for a given ground station and satellite provided in the path
        [HttpGet("next/satellite/{satelliteId:int}/groundstation/{groundStationId:int}")]
        public async Task<ActionResult<OverpassWindowDto>> GetNextOverpass(
            int satelliteId,
            int groundStationId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] double minimumElevation = 0.0)
        {
            startTime ??= DateTime.UtcNow;

            try
            {
                var result = await _overpassService.CalculateNextOverpassAsync(new NextOverpassCalculationRequestDto
                {
                    SatelliteId = satelliteId,
                    GroundStationId = groundStationId,
                    StartTime = startTime.Value,
                    MinimumElevation = minimumElevation
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, $"{ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }
    }
}