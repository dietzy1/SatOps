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
        private readonly IGroundStationService _gsService;
        private readonly IImageService _imageService;
        private readonly ILogger<GroundStationLinkController> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public GroundStationLinkController(
            IWebSocketService webSocketService,
            IGroundStationRepository gsRepository,
            IGroundStationService gsService,
            IImageService imageService,
            IConfiguration configuration,
            ILogger<GroundStationLinkController> logger)
        {
            _webSocketService = webSocketService;
            _gsRepository = gsRepository;
            _gsService = gsService;
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
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew for token validation
            };
        }

        /// <summary>
        /// Authenticates a ground station and returns an access token
        /// </summary>
        /// <param name="request">The ground station credentials</param>
        /// <returns>An access token for ground station operations</returns>
        [HttpPost("/api/v1/ground-station-link/token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetStationToken([FromBody] TokenRequestDto request)
        {
            var token = await _gsService.GenerateGroundStationTokenAsync(request);
            if (token == null) return Unauthorized("Invalid credentials.");
            return Ok(new TokenResponseDto { AccessToken = token });
        }

        /// <summary>
        /// Establishes a WebSocket connection for a ground station
        /// </summary>
        /// <remarks>
        /// The ground station must send a connect message with a valid JWT token after the WebSocket connection is established.
        /// </remarks>
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

                var connectJson = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var connectMessage = JsonSerializer.Deserialize<WebSocketConnectMessage>(connectJson);

                if (connectMessage?.Type != "connect" || string.IsNullOrEmpty(connectMessage.Token))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid hello message", CancellationToken.None);
                    return;
                }

                var handler = new JwtSecurityTokenHandler();
                ClaimsPrincipal principal;
                try
                {
                    principal = handler.ValidateToken(connectMessage.Token, _tokenValidationParameters, out _);
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

        /// <summary>
        /// Receives image data from a ground station
        /// </summary>
        /// <param name="dto">The image data including file and metadata</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("/api/v1/ground-station-link/images")]
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