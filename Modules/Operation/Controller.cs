using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SatOps.Modules.Operation
{
    [ApiController]
    [Route("api/v1/internal/operations")]
    [Authorize(Policy = Authorization.Policies.RequireGroundStation)]
    public class OperationsController(ITelemetryService telemetryService, IImageService imageService, ILogger<OperationsController> logger) : ControllerBase
    {
        [HttpPost("telemetry")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Authorization.Policies.UploadTelemetry)]
        public async Task<IActionResult> ReceiveTelemetryData([FromForm] TelemetryDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // We shouldn't really be receiving blob data here likely some other format we need to think more about this
                await telemetryService.ReceiveTelemetryDataAsync(dto);

                logger.LogInformation("Successfully received telemetry data from ground station {GroundStationId}", dto.GroundStationId);

                return Ok("Telemetry data received successfully");
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid reference in telemetry data: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive telemetry data from ground station {GroundStationId}", dto.GroundStationId);
                return StatusCode(500, "An error occurred while processing the telemetry data");
            }
        }

        [HttpPost("images")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Authorization.Policies.UploadImages)]
        public async Task<IActionResult> ReceiveImageData([FromForm] ImageDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await imageService.ReceiveImageDataAsync(dto);

                logger.LogInformation("Successfully received image data from ground station {GroundStationId} for satellite {SatelliteId}",
                    dto.GroundStationId, dto.SatelliteId);

                return Ok("Image data received successfully");
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid reference in image data: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive image data from ground station {GroundStationId}", dto.GroundStationId);
                return StatusCode(500, "An error occurred while processing the image data");
            }
        }
    }
}
