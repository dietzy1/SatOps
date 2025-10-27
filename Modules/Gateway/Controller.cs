using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SatOps.Modules.Gateway
{
    public class GatewayController : ControllerBase
    {
        private readonly IGroundStationGatewayService _gatewayService;
        private readonly IGroundStationRepository _gsRepository;
        private readonly ILogger<GatewayController> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public GatewayController(
            IGroundStationGatewayService gatewayService,
            IGroundStationRepository gsRepository,
            IConfiguration configuration,
            ILogger<GatewayController> logger)
        {
            _gatewayService = gatewayService;
            _gsRepository = gsRepository;
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

        [HttpGet("/api/v1/gs/ws")]
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
                    _logger.LogWarning("WebSocket connection closed due to invalid 'hello' message.");
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

                // Verify the token has the required scope for WebSocket connections
                var hasWebSocketScope = principal.Claims.Any(c =>
                    c.Type == "scope" && c.Value == SatOps.Authorization.Scopes.EstablishWebSocket);

                if (!hasWebSocketScope)
                {
                    _logger.LogWarning("WebSocket closed: token lacks required scope for WebSocket connection.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Insufficient permissions", CancellationToken.None);
                    return;
                }

                var subClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub");

                if (subClaim == null || !int.TryParse(subClaim.Value, out groundStationId))
                {
                    _logger.LogWarning("WebSocket closed: valid token is missing 'sub' claim for GS ID.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token claims", CancellationToken.None);
                    return;
                }

                var station = await _gsRepository.GetByIdAsync(groundStationId);
                if (station == null)
                {
                    _logger.LogWarning("GS with ID {GroundStationId} from token not found in database.", groundStationId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Ground station not registered", CancellationToken.None);
                    return;
                }

                await _gatewayService.RegisterConnection(station.Id, station.Name, webSocket);

                var confirmation = new { message = "OK", id = subClaim.Value };
                var confirmationJson = JsonSerializer.Serialize(confirmation);
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(confirmationJson), WebSocketMessageType.Text, true, CancellationToken.None);

                while (webSocket.State == WebSocketState.Open)
                {
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket for GS {GroundStationId} closed by client.", groundStationId);
                        break;
                    }
                }
            }
            catch (WebSocketException wse)
            {
                _logger.LogWarning(wse, "WebSocket exception for GS {GroundStationId}. Connection will be closed.", groundStationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WebSocket pipeline for GS {GroundStationId}.", groundStationId);
            }
            finally
            {
                if (groundStationId != -1)
                {
                    await _gatewayService.UnregisterConnection(groundStationId);
                }
            }
        }

        [HttpGet("/api/v1/gateway/status")]
        [Authorize(Policy = SatOps.Authorization.Policies.RequireAdmin)]
        public IActionResult GetStatus()
        {
            var connections = _gatewayService.GetAllConnections();

            var statuses = connections.Select(c => new ConnectionStatusDto
            {
                GroundStationId = c.GroundStationId,
                Name = c.Name,
                ConnectedAt = c.ConnectedAt,
                UptimeMinutes = (DateTime.UtcNow - c.ConnectedAt).TotalMinutes,
                LastCommandId = c.LastCommandId
            });

            return Ok(statuses);
        }
    }
}