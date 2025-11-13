using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SatOps.Data;
using SatOps.Modules.User;
using UserEntity = SatOps.Modules.User.User;

namespace SatOps.Tests.User
{
    public class UserRepositoryTests
    {
        private readonly SatOpsDbContext _dbContext;
        private readonly UserRepository _sut;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SatOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SatOpsDbContext(options);
            _sut = new UserRepository(_dbContext);
        }

        private async Task SeedDatabase()
        {
            _dbContext.Users.Add(new UserEntity { Id = 1, Name = "Admin User", Email = "admin@test.com", Auth0UserId = "auth0|admin" });
            _dbContext.Users.Add(new UserEntity { Id = 2, Name = "Viewer User", Email = "viewer@test.com", Auth0UserId = "auth0|viewer" });
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSeededUsers()
        {
            // Arrange
            await SeedDatabase();

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByAuth0UserIdAsync_ShouldReturnCorrectUser()
        {
            // Arrange
            await SeedDatabase();
            var targetAuth0Id = "auth0|viewer";

            // Act
            var result = await _sut.GetByAuth0UserIdAsync(targetAuth0Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Viewer User");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFieldsAndTimestamp()
        {
            // Arrange
            await SeedDatabase();
            var userToUpdate = await _sut.GetByIdAsync(1);
            userToUpdate!.Name = "Updated Admin Name";
            var originalTimestamp = userToUpdate.UpdatedAt;

            // Act
            var result = await _sut.UpdateAsync(userToUpdate);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Updated Admin Name");
            result.UpdatedAt.Should().BeAfter(originalTimestamp);
        }

        [Fact]
        public async Task DeleteAsync_WhenUserExists_ShouldReturnTrueAndRemoveUser()
        {
            // Arrange
            await SeedDatabase();

            // Act
            var success = await _sut.DeleteAsync(1);
            var findResult = await _sut.GetByIdAsync(1);
            var remainingUsers = await _sut.GetAllAsync();

            // Assert
            success.Should().BeTrue();
            findResult.Should().BeNull();
            remainingUsers.Should().HaveCount(1);
        }
    }
}