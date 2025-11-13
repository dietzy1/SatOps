using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SatOps.Modules.User;
using UserEntity = SatOps.Modules.User.User;


namespace SatOps.Tests.User
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly Mock<IAuth0Client> _mockAuth0Client;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _mockAuth0Client = new Mock<IAuth0Client>();
            _mockMemoryCache = new Mock<IMemoryCache>();

            _sut = new UserService(
                _mockRepo.Object,
                _mockAuth0Client.Object,
                new Mock<ILogger<UserService>>().Object,
                _mockMemoryCache.Object
            );
        }

        #region GetOrCreateUserFromAuth0Async Tests

        [Fact]
        public async Task GetOrCreateUserFromAuth0Async_WhenUserExists_ShouldReturnExistingUser()
        {
            // Arrange
            var auth0Id = "auth0|123";
            var existingUser = new UserEntity { Id = 1, Auth0UserId = auth0Id, Name = "Existing User" };
            _mockRepo.Setup(r => r.GetByAuth0UserIdAsync(auth0Id)).ReturnsAsync(existingUser);

            // Act
            var result = await _sut.GetOrCreateUserFromAuth0Async(auth0Id, "any_token");

            // Assert
            result.Should().BeEquivalentTo(existingUser);
            // Verify external client was NOT called
            _mockAuth0Client.Verify(c => c.GetUserInfoAsync(It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateUserFromAuth0Async_WhenUserDoesNotExist_ShouldFetchFromAuth0AndCreateNewUser()
        {
            // Arrange
            var auth0Id = "auth0|newuser";
            var accessToken = "valid_access_token";
            var userInfo = new Auth0UserInfo { Sub = auth0Id, Name = "New User", Email = "new@example.com" };
            var createdUser = new UserEntity { Id = 2, Auth0UserId = auth0Id, Name = "New User", Email = "new@example.com" };

            _mockRepo.Setup(r => r.GetByAuth0UserIdAsync(auth0Id)).ReturnsAsync((UserEntity?)null); // User does not exist
            _mockAuth0Client.Setup(c => c.GetUserInfoAsync(accessToken)).ReturnsAsync(userInfo);
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<UserEntity>())).ReturnsAsync(createdUser);

            // Act
            var result = await _sut.GetOrCreateUserFromAuth0Async(auth0Id, accessToken);

            // Assert
            result.Should().BeEquivalentTo(createdUser);
            // Verify dependencies were called
            _mockAuth0Client.Verify(c => c.GetUserInfoAsync(accessToken), Times.Once);
            _mockRepo.Verify(r => r.AddAsync(It.Is<UserEntity>(u =>
                u.Auth0UserId == auth0Id &&
                u.Name == userInfo.Name &&
                u.Email == userInfo.Email &&
                u.Role == UserRole.Viewer // Should default to Viewer
            )), Times.Once);
        }

        #endregion

        #region UpdateRoleAsync Tests

        [Fact]
        public async Task UpdateRoleAsync_WhenUserExists_ShouldUpdateRoleAndInvalidateCache()
        {
            // Arrange
            var userId = 1;
            var auth0Id = "auth0|123";
            var user = new UserEntity { Id = userId, Auth0UserId = auth0Id, Role = UserRole.Viewer };
            var newRole = UserRole.Operator;

            _mockRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>())).ReturnsAsync((UserEntity u) => u);

            // Act
            var result = await _sut.UpdateRoleAsync(userId, newRole);

            // Assert
            result.Should().NotBeNull();
            result!.Role.Should().Be(newRole);

            // Verify the repository was called with the updated role
            _mockRepo.Verify(r => r.UpdateAsync(It.Is<UserEntity>(u => u.Role == newRole)), Times.Once);

            // Verify the cache was invalidated for this user
            var expectedCacheKey = $"user_permissions_{auth0Id}";
            _mockMemoryCache.Verify(c => c.Remove(expectedCacheKey), Times.Once);
        }

        [Fact]
        public async Task UpdateRoleAsync_WhenUserNotFound_ShouldReturnNull()
        {
            // Arrange
            var userId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserEntity?)null);

            // Act
            var result = await _sut.UpdateRoleAsync(userId, UserRole.Admin);

            // Assert
            result.Should().BeNull();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>()), Times.Never);
            _mockMemoryCache.Verify(c => c.Remove(It.IsAny<object>()), Times.Never);
        }

        #endregion
    }
}