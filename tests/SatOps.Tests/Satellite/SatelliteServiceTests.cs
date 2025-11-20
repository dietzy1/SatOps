using Xunit;
using Moq;
using FluentAssertions;
using SatOps.Modules.Satellite;
using Microsoft.Extensions.Logging;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;

namespace SatOps.Tests.Unit.Satellite
{
    public class SatelliteServiceTests
    {
        private readonly Mock<ISatelliteRepository> _mockRepo;
        private readonly Mock<ICelestrackClient> _mockCelestrackClient;
        private readonly Mock<ILogger<SatelliteService>> _mockLogger;
        private readonly SatelliteService _sut;

        public SatelliteServiceTests()
        {
            _mockRepo = new Mock<ISatelliteRepository>();
            _mockCelestrackClient = new Mock<ICelestrackClient>();
            _mockLogger = new Mock<ILogger<SatelliteService>>();
            _sut = new SatelliteService(_mockRepo.Object, _mockCelestrackClient.Object, _mockLogger.Object);
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var sat = new SatelliteEntity { Id = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sat);

            // Act
            var result = await _sut.GetAsync(1);

            // Assert
            result.Should().Be(sat);
            // Verify we NEVER call external APIs during a Get
            _mockCelestrackClient.Verify(c => c.FetchTleAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region RefreshTleDataAsync Tests

        [Fact]
        public async Task RefreshTleDataAsync_WhenTleDataIsNew_ShouldUpdateData_AndReturnTrue()
        {
            // Arrange
            var satellite = new SatelliteEntity { Id = 1, NoradId = 25544, TleLine1 = "OLD1", TleLine2 = "OLD2" };
            var newTleData = "SATNAME\n1 NEW1\n2 NEW2";

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync(newTleData);

            // Act
            var result = await _sut.RefreshTleDataAsync(1);

            // Assert
            result.Should().BeTrue();
            // Should call the full update
            _mockRepo.Verify(r => r.UpdateTleAsync(1, "1 NEW1", "2 NEW2"), Times.Once);
            // Should NOT call the timestamp-only update
            _mockRepo.Verify(r => r.UpdateLastUpdateTimestampAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTleDataAsync_WhenTleDataIsSame_ShouldOnlyTouchTimestamp_AndReturnTrue()
        {
            // Arrange
            var satellite = new SatelliteEntity { Id = 1, NoradId = 25544, TleLine1 = "1 SAME1", TleLine2 = "2 SAME2" };
            var sameTleData = "SATNAME\n1 SAME1\n2 SAME2";

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync(sameTleData);

            // Act
            var result = await _sut.RefreshTleDataAsync(1);

            // Assert
            result.Should().BeTrue();

            // Should call timestamp update
            _mockRepo.Verify(r => r.UpdateLastUpdateTimestampAsync(1), Times.Once);

            // Should NOT call full update
            _mockRepo.Verify(r => r.UpdateTleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTleDataAsync_WhenCelestrackFails_ShouldReturnFalse()
        {
            // Arrange
            var satellite = new SatelliteEntity { Id = 1, NoradId = 25544 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync((string?)null);

            // Act
            var result = await _sut.RefreshTleDataAsync(1);

            // Assert
            result.Should().BeFalse();
            _mockRepo.Verify(r => r.UpdateTleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(r => r.UpdateLastUpdateTimestampAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion
    }
}