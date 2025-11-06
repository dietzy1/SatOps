using Microsoft.Extensions.Caching.Memory;
using SatOps.Authorization;

namespace SatOps.Modules.User
{
    public interface IUserService
    {
        Task<List<User>> ListAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByAuth0UserIdAsync(string auth0UserId);
        Task<User> GetOrCreateUserFromAuth0Async(string auth0UserId, string accessToken);
        Task<User?> UpdateAsync(int id, User entity);
        Task<User?> UpdateRoleAsync(int userId, UserRole role);
        Task<bool> DeleteAsync(int id);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IAuth0Client _auth0Client;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _cache;

        public UserService(
            IUserRepository repository,
            IAuth0Client auth0Client,
            ILogger<UserService> logger,
            IMemoryCache cache)
        {
            _repository = repository;
            _auth0Client = auth0Client;
            _logger = logger;
            _cache = cache;
        }

        public Task<List<User>> ListAsync() => _repository.GetAllAsync();

        public Task<User?> GetAsync(int id) => _repository.GetByIdAsync(id);

        public Task<User?> GetByAuth0UserIdAsync(string auth0UserId) => _repository.GetByAuth0UserIdAsync(auth0UserId);

        /// <summary>
        /// Gets a user by Auth0 ID, or creates a new user with Viewer role if they don't exist.
        /// Fetches user profile from Auth0 UserInfo endpoint for new users.
        /// </summary>
        public async Task<User> GetOrCreateUserFromAuth0Async(string auth0UserId, string accessToken)
        {
            // Try to find user by Auth0 ID first
            var user = await _repository.GetByAuth0UserIdAsync(auth0UserId);
            if (user != null)
            {
                return user;
            }

            // User doesn't exist - fetch profile from Auth0 UserInfo endpoint
            _logger.LogInformation("New user {Auth0UserId} detected, fetching profile from Auth0", auth0UserId);

            var userInfo = await _auth0Client.GetUserInfoAsync(accessToken);

            var email = userInfo.Email ?? $"{auth0UserId}@unknown.com";
            var name = userInfo.Name ?? userInfo.Nickname ?? "Unknown User";

            var newUser = new User
            {
                Auth0UserId = auth0UserId,
                Email = email,
                Name = name,
                Role = UserRole.Viewer
            };

            var createdUser = await _repository.AddAsync(newUser);

            _logger.LogInformation("Created new user {UserId} for Auth0 user {Auth0UserId}", createdUser.Id, auth0UserId);

            return createdUser;
        }

        public async Task<User?> UpdateAsync(int id, User entity)
        {
            entity.Id = id;
            return await _repository.UpdateAsync(entity);
        }

        public async Task<User?> UpdateRoleAsync(int userId, UserRole role)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.Role = role;

            var updatedUser = await _repository.UpdateAsync(user);

            if (updatedUser != null && !string.IsNullOrEmpty(updatedUser.Auth0UserId))
            {
                var cacheKey = $"user_permissions_{updatedUser.Auth0UserId}";
                _cache.Remove(cacheKey);
                _logger.LogInformation("Cache invalidated for user {UserId} (Auth0 ID: {Auth0UserId}) due to role change.", userId, updatedUser.Auth0UserId);
            }

            return updatedUser;
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
    }
}
