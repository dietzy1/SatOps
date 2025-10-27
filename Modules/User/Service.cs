namespace SatOps.Modules.User
{
    public interface IUserService
    {
        Task<List<User>> ListAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User entity);
        Task<User?> UpdateAsync(int id, User entity);
        Task<User?> UpdatePermissionsAsync(int userId, UserRole role, List<string> additionalRoles, List<string> additionalScopes);
        Task<bool> DeleteAsync(int id);
        Task<UserPermissions> GetUserPermissionsAsync(string email);
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

        public async Task<User?> UpdatePermissionsAsync(int userId, UserRole role, List<string> additionalRoles, List<string> additionalScopes)
        {
            var user = await repository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.Role = role;
            user.AdditionalRoles = additionalRoles;
            user.AdditionalScopes = additionalScopes;

            return await repository.UpdateAsync(user);
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

        private IEnumerable<string> GetDefaultScopesForRole(UserRole role)
        {
            return role switch
            {
                // Viewer: Read-only access to ground stations, satellites, and flight plans
                UserRole.Viewer =>
                [
                    Authorization.Scopes.ReadGroundStations,
                    Authorization.Scopes.ReadSatellites,
                    Authorization.Scopes.ReadFlightPlans
                ],
                // Operator: All viewer permissions + full write access to flight plans
                UserRole.Operator =>
                [
                    Authorization.Scopes.ReadGroundStations,
                    Authorization.Scopes.ReadSatellites,
                    Authorization.Scopes.ReadFlightPlans,
                    Authorization.Scopes.WriteFlightPlans
                ],
                // Admin: Full access to everything
                UserRole.Admin =>
                [
                    Authorization.Scopes.ReadGroundStations,
                    Authorization.Scopes.WriteGroundStations,
                    Authorization.Scopes.ReadSatellites,
                    Authorization.Scopes.WriteSatellites,
                    Authorization.Scopes.ReadFlightPlans,
                    Authorization.Scopes.WriteFlightPlans,
                    Authorization.Scopes.ManageUsers
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
