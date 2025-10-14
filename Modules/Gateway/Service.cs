using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SatOps.Modules.Gateway
{
    public class GroundStationConnection
    {
        public required WebSocket Socket { get; set; }
        public required int GroundStationId { get; set; }
        public required string Name { get; set; }
        public DateTime ConnectedAt { get; } = DateTime.UtcNow;
        public Guid? LastCommandId { get; set; }

        public SemaphoreSlim SendLock { get; } = new(1, 1);
    }

    public interface IGroundStationGatewayService
    {
        Task RegisterConnection(int groundStationId, string groundStationName, WebSocket socket);
        Task UnregisterConnection(int groundStationId);
        bool IsGroundStationConnected(int groundStationId);
        Task SendScheduledCommand(int groundStationId, string satelliteName, DateTime executionTime, List<string> cshScript);
        IEnumerable<GroundStationConnection> GetAllConnections();
    }

    public class GroundStationGatewayService(ILogger<GroundStationGatewayService> logger) : IGroundStationGatewayService
    {
        private readonly ConcurrentDictionary<int, GroundStationConnection> _connections = new();

        public Task RegisterConnection(int groundStationId, string groundStationName, WebSocket socket)
        {
            var connection = new GroundStationConnection
            {
                Socket = socket,
                GroundStationId = groundStationId,
                Name = groundStationName
            };

            logger.LogInformation("Registering connection for GS {GroundStationId} ({Name})", connection.GroundStationId, connection.Name);
            _connections[groundStationId] = connection;
            return Task.CompletedTask;
        }

        public Task UnregisterConnection(int groundStationId)
        {
            if (_connections.TryRemove(groundStationId, out var connection))
            {
                logger.LogInformation("Unregistering connection for GS {GroundStationId} ({Name})", connection.GroundStationId, connection.Name);
            }
            return Task.CompletedTask;
        }

        public bool IsGroundStationConnected(int groundStationId)
        {
            return _connections.TryGetValue(groundStationId, out var connection) && connection.Socket.State == WebSocketState.Open;
        }

        public IEnumerable<GroundStationConnection> GetAllConnections()
        {
            return _connections.Values.ToList();
        }

        public async Task SendScheduledCommand(int groundStationId, string satelliteName, DateTime executionTime, List<string> cshScript)
        {
            if (!_connections.TryGetValue(groundStationId, out var connection) || connection.Socket.State != WebSocketState.Open)
            {
                logger.LogWarning("Attempted to send command to disconnected GS ID: {GroundStationId}", groundStationId);
                throw new InvalidOperationException($"Ground station {groundStationId} is not connected.");
            }

            await connection.SendLock.WaitAsync();
            try
            {
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

                connection.LastCommandId = message.RequestId;

                var messageJson = JsonSerializer.Serialize(message);
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);

                var scriptJson = JsonSerializer.Serialize(cshScript);
                var scriptBytes = Encoding.UTF8.GetBytes(scriptJson);

                logger.LogInformation("Sending scheduled command {RequestId} to GS {GroundStationId} ({Name})", message.RequestId, groundStationId, connection.Name);

                await connection.Socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                await connection.Socket.SendAsync(new ArraySegment<byte>(scriptBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }
    }
}