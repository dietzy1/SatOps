using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SatOps.Modules.Operation
{
    [ApiController]
    [Route("api/internal/operations")]
    [Authorize(Policy = "RequireGroundStation")]
    public class OperationsController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly IImageService _imageService;
        private readonly ILogger<OperationsController> _logger;

        public OperationsController(ITelemetryService telemetryService, IImageService imageService, ILogger<OperationsController> logger)
        {
            _telemetryService = telemetryService;
            _imageService = imageService;
            _logger = logger;
        }

        [HttpPost("telemetry")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReceiveTelemetryData([FromForm] TelemetryDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // We shouldn't really be receiving blob data here likely some other format we need to think more about this
                await _telemetryService.ReceiveTelemetryDataAsync(dto);

                _logger.LogInformation("Successfully received telemetry data from ground station {GroundStationId}", dto.GroundStationId);

                return Ok("Telemetry data received successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid reference in telemetry data: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive telemetry data from ground station {GroundStationId}", dto.GroundStationId);
                return StatusCode(500, "An error occurred while processing the telemetry data");
            }
        }

        [HttpPost("images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReceiveImageData([FromForm] ImageDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _imageService.ReceiveImageDataAsync(dto);

                _logger.LogInformation("Successfully received image data from ground station {GroundStationId} for satellite {SatelliteId}",
                    dto.GroundStationId, dto.SatelliteId);

                return Ok("Image data received successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid reference in image data: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive image data from ground station {GroundStationId}", dto.GroundStationId);
                return StatusCode(500, "An error occurred while processing the image data");
            }
        }
    }
}
