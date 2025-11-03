namespace SatOps.Modules.User
{
    public interface IUserService
    {
        Task<List<User>> ListAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByAuth0UserIdAsync(string auth0UserId);
        Task<User> GetOrCreateUserFromAuth0Async(string auth0UserId, string email, string name);
        Task<User?> UpdateAsync(int id, User entity);
        Task<User?> UpdateRoleAsync(int userId, UserRole role);
        Task<bool> DeleteAsync(int id);
    }

    public class UserService(IUserRepository repository) : IUserService
    {
        public Task<List<User>> ListAsync() => repository.GetAllAsync();

        public Task<User?> GetAsync(int id) => repository.GetByIdAsync(id);

        public Task<User?> GetByAuth0UserIdAsync(string auth0UserId) => repository.GetByAuth0UserIdAsync(auth0UserId);

        /// <summary>
        /// Gets a user by Auth0 ID, or creates a new user with Viewer role if they don't exist
        /// </summary>
        public async Task<User> GetOrCreateUserFromAuth0Async(string auth0UserId, string email, string name)
        {
            // Try to find user by Auth0 ID first
            var user = await repository.GetByAuth0UserIdAsync(auth0UserId);
            if (user != null)
            {
                return user;
            }

            var newUser = new User
            {
                Auth0UserId = auth0UserId,
                Email = email,
                Name = name,
                Role = UserRole.Viewer
            };

            return await repository.AddAsync(newUser);
        }

        public async Task<User?> UpdateAsync(int id, User entity)
        {
            entity.Id = id;
            return await repository.UpdateAsync(entity);
        }

        public async Task<User?> UpdateRoleAsync(int userId, UserRole role)
        {
            var user = await repository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.Role = role;

            return await repository.UpdateAsync(user);
        }

        public Task<bool> DeleteAsync(int id) => repository.DeleteAsync(id);
    }
}
