using Microsoft.EntityFrameworkCore;
using SatOps.Data;

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
    }

    public class UserRepository(SatOpsDbContext dbContext) : IUserRepository
    {
        public Task<List<User>> GetAllAsync()
        {
            return dbContext.Users.AsNoTracking().ToListAsync();
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            dbContext.Users.Add(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<User?> UpdateAsync(User entity)
        {
            var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == entity.Id);
            if (existing == null)
            {
                return null;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            entity.CreatedAt = existing.CreatedAt; // Preserve original creation date

            dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return false;
            }

            dbContext.Users.Remove(existing);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
