using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SatOps.Modules.Gateway;
using System.Net.WebSockets;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace SatOps.Tests
{
    public class GatewayServiceTests
    {
        private readonly Mock<ILogger<GroundStationGatewayService>> _mockLogger;
        private readonly GroundStationGatewayService _sut;

        public GatewayServiceTests()
        {
            _mockLogger = new Mock<ILogger<GroundStationGatewayService>>();
            _sut = new GroundStationGatewayService(_mockLogger.Object);
        }

        /// <summary>
        /// Helper method to create a mock WebSocket that is in the 'Open' state.
        /// </summary>
        private Mock<WebSocket> CreateOpenMockWebSocket()
        {
            var mockSocket = new Mock<WebSocket>();
            mockSocket.SetupGet(s => s.State).Returns(WebSocketState.Open);
            return mockSocket;
        }

        [Fact]
        public async Task RegisterConnection_WhenCalled_AddsConnectionToInternalDictionary()
        {
            // Arrange
            var mockSocket = CreateOpenMockWebSocket();
            var groundStationId = 1;
            var groundStationName = "Test-GS-1";

            // Act
            await _sut.RegisterConnection(groundStationId, groundStationName, mockSocket.Object);
            var isConnected = _sut.IsGroundStationConnected(groundStationId);
            var allConnections = _sut.GetAllConnections();

            // Assert
            isConnected.Should().BeTrue();
            allConnections.Should().ContainSingle(c => c.GroundStationId == groundStationId && c.Name == groundStationName);
        }

        [Fact]
        public async Task UnregisterConnection_WhenCalled_RemovesConnectionFromInternalDictionary()
        {
            // Arrange
            var mockSocket = CreateOpenMockWebSocket();
            var groundStationId = 2;
            await _sut.RegisterConnection(groundStationId, "Test-GS-2", mockSocket.Object);

            // Act
            await _sut.UnregisterConnection(groundStationId);
            var isConnected = _sut.IsGroundStationConnected(groundStationId);

            // Assert
            isConnected.Should().BeFalse();
        }

        [Fact]
        public async Task SendScheduledCommand_ToConnectedStation_SendsTwoMessagesWithCorrectContent()
        {
            // Arrange
            var mockSocket = CreateOpenMockWebSocket();
            var sentMessages = new ConcurrentBag<byte[]>();

            mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, CancellationToken.None))
                .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>((buffer, type, endOfMessage, token) =>
                {
                    sentMessages.Add(buffer.ToArray());
                })
                .Returns(Task.CompletedTask);

            var groundStationId = 3;
            await _sut.RegisterConnection(groundStationId, "Test-GS-3", mockSocket.Object);

            var satelliteName = "DISCO-1";
            var executionTime = DateTime.UtcNow.AddHours(1);
            var cshScript = new List<string> { "ident", "ping 1" };

            // Act
            await _sut.SendScheduledCommand(groundStationId, satelliteName, executionTime, cshScript);

            // Assert
            // Verify that SendAsync was called exactly twice.
            mockSocket.Verify(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, CancellationToken.None), Times.Exactly(2));
            sentMessages.Count.Should().Be(2);

            // Convert both messages to strings
            var sentMessagesAsStrings = sentMessages.Select(bytes => Encoding.UTF8.GetString(bytes)).ToList();

            // Identify the header (JSON object) and script (JSON array) by their structure, not by order.
            var headerMessageJson = sentMessagesAsStrings.FirstOrDefault(s => s.Trim().StartsWith("{"));
            var scriptMessageJson = sentMessagesAsStrings.FirstOrDefault(s => s.Trim().StartsWith("["));

            // Ensure we found both types of messages
            headerMessageJson.Should().NotBeNull("the header message (a JSON object) should have been sent");
            scriptMessageJson.Should().NotBeNull("the script message (a JSON array) should have been sent");

            // Deserialize and verify the header message.
            var headerMessage = JsonSerializer.Deserialize<ScheduleTransmissionMessage>(headerMessageJson!);

            headerMessage.Should().NotBeNull();
            headerMessage!.Type.Should().Be("schedule_transmission");
            headerMessage.Frames.Should().Be(1);
            headerMessage.Data.Satellite.Should().Be(satelliteName);
            // Use BeCloseTo for DateTime comparisons to avoid precision issues in tests
            JsonSerializer.Deserialize<DateTime>(JsonSerializer.Serialize(headerMessage.Data.Time))
                .Should().BeCloseTo(executionTime, precision: TimeSpan.FromSeconds(1));

            // Deserialize and verify the script message.
            var scriptMessage = JsonSerializer.Deserialize<List<string>>(scriptMessageJson!);

            scriptMessage.Should().NotBeNull();
            scriptMessage.Should().BeEquivalentTo(cshScript);
        }

        [Fact]
        public async Task SendScheduledCommand_ToDisconnectedStation_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = 99;

            // Act
            Func<Task> act = () => _sut.SendScheduledCommand(nonExistentId, "any", DateTime.UtcNow, new List<string>());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Ground station {nonExistentId} is not connected.");
        }

        [Fact]
        public async Task SendScheduledCommand_WhenCalledConcurrently_IsThreadSafeAndDoesNotThrow()
        {
            // Arrange
            var mockSocket = CreateOpenMockWebSocket();
            var sendCount = 0;

            // Setup a mock send that has a small artificial delay. This makes it more likely
            // for a race condition to occur if the semaphore is NOT working.
            mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, CancellationToken.None))
                .Callback(() => Interlocked.Increment(ref sendCount))
                .Returns(async () => await Task.Delay(20));

            var groundStationId = 4;
            await _sut.RegisterConnection(groundStationId, "Test-GS-4", mockSocket.Object);

            // Act: Start two commands to the SAME ground station at the same time.
            var task1 = _sut.SendScheduledCommand(groundStationId, "SAT-A", DateTime.UtcNow, new List<string> { "cmd1" });
            var task2 = _sut.SendScheduledCommand(groundStationId, "SAT-B", DateTime.UtcNow, new List<string> { "cmd2" });

            // Wait for both tasks to complete
            await Task.WhenAll(task1, task2);

            // Assert
            // Each of the two commands should have sent 2 messages (header + script).
            sendCount.Should().Be(4);
        }
    }
}