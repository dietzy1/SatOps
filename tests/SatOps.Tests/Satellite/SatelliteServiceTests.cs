using Xunit;
using Moq;
using FluentAssertions;
using SatOps.Modules.Satellite;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;

namespace SatOps.Tests.Unit.Satellite
{
    public class SatelliteServiceTests
    {
        private readonly Mock<ISatelliteRepository> _mockRepo;
        private readonly Mock<ICelestrackClient> _mockCelestrackClient;
        private readonly SatelliteService _sut;

        public SatelliteServiceTests()
        {
            _mockRepo = new Mock<ISatelliteRepository>();
            _mockCelestrackClient = new Mock<ICelestrackClient>();
            _sut = new SatelliteService(_mockRepo.Object, _mockCelestrackClient.Object);
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_WhenTleIsRecent_ShouldNotCallCelestrack()
        {
            // Arrange
            var satellite = new SatelliteEntity
            {
                Id = 1,
                NoradId = 25544,
                LastUpdate = DateTime.UtcNow.AddHours(-1) // TLE data is only 1 hour old
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);

            // Act
            var result = await _sut.GetAsync(1);

            // Assert
            result.Should().NotBeNull();
            // Crucially, verify that the external client was NOT called
            _mockCelestrackClient.Verify(c => c.FetchTleAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.UpdateTleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_WhenTleIsOld_ShouldFetchAndUpdateFromCelestrack()
        {
            // Arrange
            var satellite = new SatelliteEntity
            {
                Id = 1,
                NoradId = 25544,
                LastUpdate = DateTime.UtcNow.AddHours(-7) // TLE data is 7 hours old
            };
            var tleDataFromCelestrack = "SATNAME\n1 NEWLINE1\n2 NEWLINE2";

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync(tleDataFromCelestrack);

            // Act
            var result = await _sut.GetAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.TleLine1.Should().Be("1 NEWLINE1");
            result.TleLine2.Should().Be("2 NEWLINE2");

            // Verify dependencies were called
            _mockCelestrackClient.Verify(c => c.FetchTleAsync(satellite.NoradId), Times.Once);
            _mockRepo.Verify(r => r.UpdateTleAsync(1, "1 NEWLINE1", "2 NEWLINE2"), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WhenSatelliteNotFound_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((SatelliteEntity?)null);

            // Act
            var result = await _sut.GetAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region RefreshTleDataAsync Tests

        [Fact]
        public async Task RefreshTleDataAsync_WhenTleDataIsNew_ShouldUpdateAndReturnTrue()
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
            _mockRepo.Verify(r => r.UpdateTleAsync(1, "1 NEW1", "2 NEW2"), Times.Once);
        }

        [Fact]
        public async Task RefreshTleDataAsync_WhenTleDataIsSame_ShouldNotUpdateAndReturnFalse()
        {
            // Arrange
            var satellite = new SatelliteEntity { Id = 1, NoradId = 25544, TleLine1 = "1 SAME1", TleLine2 = "2 SAME2" };
            var sameTleData = "SATNAME\n1 SAME1\n2 SAME2";

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync(sameTleData);

            // Act
            var result = await _sut.RefreshTleDataAsync(1);

            // Assert
            result.Should().BeFalse();
            _mockRepo.Verify(r => r.UpdateTleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTleDataAsync_WhenCelestrackFails_ShouldReturnFalse()
        {
            // Arrange
            var satellite = new SatelliteEntity { Id = 1, NoradId = 25544 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(satellite);
            _mockCelestrackClient.Setup(c => c.FetchTleAsync(satellite.NoradId)).ReturnsAsync((string?)null); // Simulate API failure

            // Act
            var result = await _sut.RefreshTleDataAsync(1);

            // Assert
            result.Should().BeFalse();
            _mockRepo.Verify(r => r.UpdateTleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion
    }
}