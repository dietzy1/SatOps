using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SatOps.Modules.Groundstation;
using SatOps.Modules.GroundStationLink;

namespace SatOps.Tests.Unit.Groundstation
{
    public class GroundstationServiceTests
    {
        private readonly Mock<IGroundStationRepository> _mockRepo;
        private readonly Mock<IWebSocketService> _mockGatewayService;
        private readonly IConfiguration _configuration;
        private readonly GroundStationService _sut;

        public GroundstationServiceTests()
        {
            _mockRepo = new Mock<IGroundStationRepository>();
            _mockGatewayService = new Mock<IWebSocketService>();

            // Mock IConfiguration for JWT settings
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "a-very-secret-key-that-is-long-enough-for-hs256"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpirationHours", "1"}
            };
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

            _sut = new GroundStationService(
                _mockRepo.Object,
                _mockGatewayService.Object,
                _configuration,
                new Mock<ILogger<GroundStationService>>().Object
            );
        }

        [Fact]
        public async Task ListAsync_ShouldSetConnectedStatusFromGatewayService()
        {
            // Arrange
            var stationsInDb = new List<GroundStation>
            {
                new() { Id = 1, Name = "GS-1" }, // Should be connected
                new() { Id = 2, Name = "GS-2" }, // Should be disconnected
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(stationsInDb);
            _mockGatewayService.Setup(g => g.IsGroundStationConnected(1)).Returns(true);
            _mockGatewayService.Setup(g => g.IsGroundStationConnected(2)).Returns(false);

            // Act
            var result = await _sut.ListAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First(s => s.Id == 1).Connected.Should().BeTrue();
            result.First(s => s.Id == 2).Connected.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateApiKeyAndSaveHashedVersion()
        {
            // Arrange
            var inputStation = new GroundStation { Name = "New Station", Location = new Location() };
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<GroundStation>())).ReturnsAsync((GroundStation gs) =>
            {
                gs.Id = 99; // Simulate DB generating an ID
                return gs;
            });

            // Act
            var (createdStation, rawApiKey) = await _sut.CreateAsync(inputStation);

            // Assert
            rawApiKey.Should().NotBeNullOrEmpty();
            createdStation.Should().NotBeNull();
            createdStation.Id.Should().Be(99);

            // Verify that the saved entity has a hash, not the raw key
            _mockRepo.Verify(r => r.AddAsync(It.Is<GroundStation>(gs =>
                !string.IsNullOrEmpty(gs.ApiKeyHash) &&
                gs.ApiKeyHash != rawApiKey &&
                gs.ApplicationId != Guid.Empty
            )), Times.Once);
        }

        [Fact]
        public async Task GenerateGroundStationTokenAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var rawKey = "my-secret-key";
            var hashedKey = ApiKey.Create(rawKey);
            var appId = Guid.NewGuid();
            var station = new GroundStation { Id = 1, ApplicationId = appId, ApiKeyHash = hashedKey.Hash };
            var request = new TokenRequestDto { ApplicationId = appId, ApiKey = rawKey };

            _mockRepo.Setup(r => r.GetByApplicationIdAsync(appId)).ReturnsAsync(station);

            // Act
            var token = await _sut.GenerateGroundStationTokenAsync(request);

            // Assert
            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GenerateGroundStationTokenAsync_ShouldReturnNull_WhenApiKeyIsInvalid()
        {
            // Arrange
            var rawKey = "my-secret-key";
            var hashedKey = ApiKey.Create(rawKey);
            var appId = Guid.NewGuid();
            var station = new GroundStation { Id = 1, ApplicationId = appId, ApiKeyHash = hashedKey.Hash };
            var request = new TokenRequestDto { ApplicationId = appId, ApiKey = "wrong-key" };

            _mockRepo.Setup(r => r.GetByApplicationIdAsync(appId)).ReturnsAsync(station);

            // Act
            var token = await _sut.GenerateGroundStationTokenAsync(request);

            // Assert
            token.Should().BeNull();
        }
    }
}