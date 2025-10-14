namespace SatOps.Modules.User
{
    public interface IUserService
    {
        Task<List<User>> ListAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User entity);
        Task<User?> UpdateAsync(int id, User entity);
        Task<bool> DeleteAsync(int id);

        // RBAC methods for "default lowest role + elevated permissions" model
        Task<UserPermissions> GetUserPermissionsAsync(string email);
        Task<bool> GrantAdditionalPermissionsAsync(int userId, List<string> additionalScopes, List<string> additionalRoles);
        Task<bool> HasPermissionAsync(string email, string requiredPermission);
        Task<bool> HasRoleAsync(string email, string requiredRole);
    }

    public class UserService(IUserRepository repository) : IUserService
    {
        public Task<List<User>> ListAsync() => repository.GetAllAsync();

        public Task<User?> GetAsync(int id) => repository.GetByIdAsync(id);

        public Task<User?> GetByEmailAsync(string email) => repository.GetByEmailAsync(email);

        public Task<User> CreateAsync(User entity) => repository.AddAsync(entity);

        public async Task<User?> UpdateAsync(int id, User entity)
        {
            entity.Id = id;
            return await repository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => repository.DeleteAsync(id);

        public async Task<UserPermissions> GetUserPermissionsAsync(string email)
        {
            var user = await repository.GetByEmailAsync(email);
            if (user == null)
            {
                return new UserPermissions
                {
                    BaseRole = UserRole.Viewer,
                    AllRoles = ["Viewer"],
                    AllScopes = []
                };
            }

            var allRoles = new List<string> { user.Role.ToString() };
            allRoles.AddRange(user.AdditionalRoles);

            var allScopes = GetDefaultScopesForRole(user.Role).ToList();
            allScopes.AddRange(user.AdditionalScopes);

            return new UserPermissions
            {
                UserId = user.Id,
                Email = user.Email,
                BaseRole = user.Role,
                AllRoles = allRoles.Distinct().ToList(),
                AllScopes = allScopes.Distinct().ToList()
            };
        }

        public async Task<bool> GrantAdditionalPermissionsAsync(int userId, List<string> additionalScopes, List<string> additionalRoles)
        {
            return await repository.UpdateAdditionalPermissionsAsync(userId, additionalScopes, additionalRoles);
        }

        public async Task<bool> HasPermissionAsync(string email, string requiredPermission)
        {
            var permissions = await GetUserPermissionsAsync(email);
            return permissions.AllScopes.Contains(requiredPermission);
        }

        public async Task<bool> HasRoleAsync(string email, string requiredRole)
        {
            var permissions = await GetUserPermissionsAsync(email);
            return permissions.AllRoles.Contains(requiredRole);
        }

        private IEnumerable<string> GetDefaultScopesForRole(UserRole role)
        {
            return role switch
            {
                UserRole.Viewer => ["read:ground-stations", "read:satellites", "read:flight-plans"],
                UserRole.Operator =>
                [
                    "read:ground-stations", "read:satellites", "read:flight-plans",
                    "write:flight-plans", "approve:flight-plans"
                ],
                UserRole.Admin =>
                [
                    "read:ground-stations", "read:satellites", "read:flight-plans",
                    "write:ground-stations", "write:satellites", "write:flight-plans",
                    "delete:ground-stations", "delete:satellites", "delete:flight-plans",
                    "approve:flight-plans", "manage:users"
                ],
                _ => Array.Empty<string>()
            };
        }
    }

    public class UserPermissions
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public UserRole BaseRole { get; set; }
        public List<string> AllRoles { get; set; } = [];
        public List<string> AllScopes { get; set; } = [];
    }
}
