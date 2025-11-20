using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SatOps.Data;
using SatOps.Modules.Satellite;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;


namespace SatOps.Tests.Satellite
{
    public class SatelliteRepositoryTests
    {
        private readonly SatOpsDbContext _dbContext;
        private readonly SatelliteRepository _sut;

        public SatelliteRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SatOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SatOpsDbContext(options);
            _sut = new SatelliteRepository(_dbContext);
        }

        private async Task SeedDatabase()
        {
            _dbContext.Satellites.Add(new SatelliteEntity { Id = 1, Name = "ISS", NoradId = 25544 });
            _dbContext.Satellites.Add(new SatelliteEntity { Id = 2, Name = "Hubble", NoradId = 20580 });
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSeededSatellites()
        {
            // Arrange
            await SeedDatabase();

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectSatellite_WhenIdExists()
        {
            // Arrange
            await SeedDatabase();

            // Act
            var result = await _sut.GetByIdAsync(2);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Hubble");
        }

        [Fact]
        public async Task UpdateTleAsync_ShouldUpdateTleLinesAndLastUpdateTimestamp()
        {
            // Arrange
            await SeedDatabase();
            var satelliteId = 1;
            var originalSatellite = await _dbContext.Satellites.AsNoTracking().FirstAsync(s => s.Id == satelliteId);
            var originalTimestamp = originalSatellite.LastUpdate;

            var newTle1 = "1 NEW-TLE-LINE-1";
            var newTle2 = "2 NEW-TLE-LINE-2";

            // Act
            await _sut.UpdateTleAsync(satelliteId, newTle1, newTle2);

            // Assert
            // Use a separate context instance or AsNoTracking to ensure we get the saved data
            var updatedSatellite = await _dbContext.Satellites.AsNoTracking().FirstAsync(s => s.Id == satelliteId);

            updatedSatellite.TleLine1.Should().Be(newTle1);
            updatedSatellite.TleLine2.Should().Be(newTle2);
            updatedSatellite.LastUpdate.Should().BeAfter(originalTimestamp);
        }

        [Fact]
        public async Task UpdateLastUpdateTimestampAsync_ShouldOnlyUpdateTimestamp()
        {
            // Arrange
            await SeedDatabase();
            var id = 1;
            var original = await _dbContext.Satellites.AsNoTracking().FirstAsync(s => s.Id == id);
            var oldTle = original.TleLine1;

            // Act
            await _sut.UpdateLastUpdateTimestampAsync(id);

            // Assert
            var updated = await _dbContext.Satellites.AsNoTracking().FirstAsync(s => s.Id == id);
            updated.LastUpdate.Should().BeAfter(original.LastUpdate);
            updated.TleLine1.Should().Be(oldTle);
        }
    }
}