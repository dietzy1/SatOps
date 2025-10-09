using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SatOps.Modules.Gateway
{
    public interface IGroundStationGatewayService
    {
        Task RegisterConnection(int groundStationId, WebSocket socket);
        Task UnregisterConnection(int groundStationId);
        bool IsGroundStationConnected(int groundStationId);
        Task SendScheduledCommand(int groundStationId, string satelliteName, DateTime executionTime, List<string> cshScript);
    }

    public class GroundStationGatewayService : IGroundStationGatewayService
    {
        private readonly ConcurrentDictionary<int, WebSocket> _connections = new();
        private readonly ILogger<GroundStationGatewayService> _logger;

        public GroundStationGatewayService(ILogger<GroundStationGatewayService> logger)
        {
            _logger = logger;
        }

        public Task RegisterConnection(int groundStationId, WebSocket socket)
        {
            _logger.LogInformation("Registering connection for Ground Station ID: {GroundStationId}", groundStationId);
            _connections[groundStationId] = socket;
            return Task.CompletedTask;
        }

        public Task UnregisterConnection(int groundStationId)
        {
            _logger.LogInformation("Unregistering connection for Ground Station ID: {GroundStationId}", groundStationId);
            _connections.TryRemove(groundStationId, out _);
            return Task.CompletedTask;
        }

        public bool IsGroundStationConnected(int groundStationId)
        {
            return _connections.ContainsKey(groundStationId) && _connections[groundStationId].State == WebSocketState.Open;
        }

        public async Task SendScheduledCommand(int groundStationId, string satelliteName, DateTime executionTime, List<string> cshScript)
        {
            if (!IsGroundStationConnected(groundStationId))
            {
                _logger.LogWarning("Attempted to send scheduled command to disconnected Ground Station ID: {GroundStationId}", groundStationId);
                throw new InvalidOperationException($"Ground station {groundStationId} is not connected.");
            }

            var socket = _connections[groundStationId];

            var message = new ScheduleTransmissionMessage
            {
                RequestId = Guid.NewGuid(),
                Type = "schedule_transmission",
                Frames = 1,
                Data = new ScheduleTransmissionData
                {
                    Satellite = satelliteName,
                    Time = executionTime.ToString("o")
                }
            };

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            var scriptJson = JsonSerializer.Serialize(cshScript);
            var scriptBytes = Encoding.UTF8.GetBytes(scriptJson);

            _logger.LogInformation("Sending scheduled command to GS {GroundStationId} for execution at {ExecutionTime}", groundStationId, executionTime);

            await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.SendAsync(new ArraySegment<byte>(scriptBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}