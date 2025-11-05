using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SatOps.Modules.GroundStationLink
{
    [ApiController]
    public class GroundStationLinkController : ControllerBase
    {
        private readonly IWebSocketService _webSocketService;
        private readonly IGroundStationRepository _gsRepository;
        private readonly ITelemetryService _telemetryService;
        private readonly IImageService _imageService;
        private readonly ILogger<GroundStationLinkController> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public GroundStationLinkController(
            IWebSocketService webSocketService,
            IGroundStationRepository gsRepository,
            ITelemetryService telemetryService,
            IImageService imageService,
            IConfiguration configuration,
            ILogger<GroundStationLinkController> logger)
        {
            _webSocketService = webSocketService;
            _gsRepository = gsRepository;
            _telemetryService = telemetryService;
            _imageService = imageService;
            _logger = logger;

            var jwtSettings = configuration.GetSection("Jwt");
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        [HttpGet("/api/v1/ground-station-link/connect")]
        public async Task HandleConnection()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            int groundStationId = -1;

            try
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Close) return;

                var helloJson = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var helloMessage = JsonSerializer.Deserialize<HelloMessageDto>(helloJson);

                if (helloMessage?.Type != "hello" || string.IsNullOrEmpty(helloMessage.Token))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid hello message", CancellationToken.None);
                    return;
                }

                var handler = new JwtSecurityTokenHandler();
                ClaimsPrincipal principal;
                try
                {
                    principal = handler.ValidateToken(helloMessage.Token, _tokenValidationParameters, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WebSocket connection closed due to invalid token.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", CancellationToken.None);
                    return;
                }

                var hasWebSocketScope = principal.Claims.Any(c => c.Type == "scope" && c.Value == Authorization.GroundStationScopes.EstablishWebSocket);
                if (!hasWebSocketScope)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Insufficient permissions", CancellationToken.None);
                    return;
                }

                var subClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub");
                if (subClaim == null || !int.TryParse(subClaim.Value, out groundStationId))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token claims", CancellationToken.None);
                    return;
                }

                var station = await _gsRepository.GetByIdAsync(groundStationId);
                if (station == null)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Ground station not registered", CancellationToken.None);
                    return;
                }

                await _webSocketService.RegisterConnection(station.Id, station.Name, webSocket);
                var confirmation = new { message = "OK", id = subClaim.Value };
                var confirmationJson = JsonSerializer.Serialize(confirmation);
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(confirmationJson), WebSocketMessageType.Text, true, CancellationToken.None);

                while (webSocket.State == WebSocketState.Open)
                {
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                }
            }
            catch (WebSocketException wse)
            {
                _logger.LogWarning(wse, "WebSocket exception for GS {GroundStationId}.", groundStationId);
            }
            finally
            {
                if (groundStationId != -1)
                {
                    await _webSocketService.UnregisterConnection(groundStationId);
                }
            }
        }

        [HttpPost("/api/v1/internal/ground-station-link/telemetry")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Authorization.Policies.RequireGroundStation)]
        public async Task<IActionResult> ReceiveTelemetryData([FromForm] TelemetryDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                await _telemetryService.ReceiveTelemetryDataAsync(dto);
                return Ok("Telemetry data received successfully");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive telemetry data from ground station {GroundStationId}", dto.GroundStationId);
                return StatusCode(500, "An error occurred while processing the telemetry data");
            }
        }

        [HttpPost("/api/v1/internal/ground-station-link/images")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(105 * 1024 * 1024)]
        [Authorize(Policy = Authorization.Policies.RequireGroundStation)]
        public async Task<IActionResult> ReceiveImageData([FromForm] ImageDataReceiveDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                await _imageService.ReceiveImageDataAsync(dto);
                return Ok("Image data received successfully");
            }
            catch (ArgumentException ex)
            {
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