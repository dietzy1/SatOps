using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SatOps.Data;
using SatOps.Modules.Groundstation;

namespace SatOps.Tests.Integration.Groundstation
{
    public class GroundstationRepositoryTests
    {
        private readonly SatOpsDbContext _dbContext;
        private readonly GroundStationRepository _sut;

        public GroundstationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SatOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SatOpsDbContext(options);
            _sut = new GroundStationRepository(_dbContext);
        }

        private async Task SeedDatabase()
        {
            _dbContext.GroundStations.Add(new GroundStation { Id = 1, Name = "Aarhus", ApplicationId = Guid.NewGuid() });
            _dbContext.GroundStations.Add(new GroundStation { Id = 2, Name = "Svalbard", ApplicationId = Guid.NewGuid() });
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSeededStations()
        {
            // Arrange
            await SeedDatabase();

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByApplicationIdAsync_ShouldReturnCorrectStation()
        {
            // Arrange
            var targetAppId = Guid.NewGuid();
            _dbContext.GroundStations.Add(new GroundStation { Id = 3, Name = "Target", ApplicationId = targetAppId });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetByApplicationIdAsync(targetAppId);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Target");
        }
    }
}