using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.User
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User entity);
        Task<User?> UpdateAsync(User entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateAdditionalPermissionsAsync(int id, List<string> additionalScopes, List<string> additionalRoles);
    }

    public class UserRepository : IUserRepository
    {
        private readonly SatOpsDbContext _dbContext;

        public UserRepository(SatOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<User>> GetAllAsync()
        {
            return _dbContext.Users.AsNoTracking().ToListAsync();
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _dbContext.Users.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<User?> UpdateAsync(User entity)
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == entity.Id);
            if (existing == null)
            {
                return null;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            entity.CreatedAt = existing.CreatedAt; // Preserve original creation date

            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return false;
            }

            _dbContext.Users.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAdditionalPermissionsAsync(int id, List<string> additionalScopes, List<string> additionalRoles)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            user.AdditionalScopes = additionalScopes;
            user.AdditionalRoles = additionalRoles;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
