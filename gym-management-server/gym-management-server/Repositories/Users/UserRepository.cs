using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly GymManagementContext _db;

        public UserRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.GoogleId == googleId && x.IsActive);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Username == username && x.IsActive);
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }
    }
}
