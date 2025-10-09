using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SatOps.Modules.Gateway
{
    public class GatewayController : ControllerBase
    {
        private readonly IGroundStationGatewayService _gatewayService;
        private readonly ILogger<GatewayController> _logger;

        public GatewayController(IGroundStationGatewayService gatewayService, ILogger<GatewayController> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        [HttpGet("/api/gs/ws")]
        public async Task HandleConnection()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
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

                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(helloMessage.Token);

                    var subClaim = token.Claims.FirstOrDefault(c => c.Type == "sub");

                    if (subClaim == null || !int.TryParse(subClaim.Value, out groundStationId))
                    {
                        _logger.LogWarning("WebSocket connection closed due to invalid or missing 'sub' claim in token.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", CancellationToken.None);
                        return;
                    }

                    await _gatewayService.RegisterConnection(groundStationId, webSocket);

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
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}