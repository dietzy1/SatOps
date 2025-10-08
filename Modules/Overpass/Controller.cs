using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Overpass
{
    [ApiController]
    [Route("api/v1/overpasses")]
    public class OverpassesController : ControllerBase
    {
        private readonly IOverpassService _overpassService;

        public OverpassesController(IOverpassService overpassService)
        {
            _overpassService = overpassService;
        }

        [HttpGet("satellite/{satelliteId:int}/groundstation/{groundStationId:int}")]
        public async Task<ActionResult<List<OverpassWindowDto>>> GetOverpassWindows(
            int satelliteId,
            int groundStationId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] double minimumElevation = 0.0,
            [FromQuery] int? maxResults = null,
            [FromQuery] int? minimumDuration = null)
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
                    MinimumElevation = minimumElevation,
                    MaxResults = maxResults,
                    MinimumDurationSeconds = minimumDuration,
                });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}