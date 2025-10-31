using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SatOps.Modules.Overpass
{
    [ApiController]
    [Route("api/v1/overpasses")]
    [Authorize]
    public class OverpassesController(IOverpassService overpassService) : ControllerBase
    {
        [HttpGet("satellite/{satelliteId:int}/groundstation/{groundStationId:int}")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
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
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Time Window",
                    Detail = $"End time ({endTime:O}) must be after start time ({startTime:O}).",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                });
            }

            try
            {
                var result = await overpassService.CalculateOverpassesAsync(new OverpassWindowsCalculationRequestDto
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