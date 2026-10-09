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

        // Username/Email/GoogleId are global, not per-tenant (see GymManagementContext's User
        // configuration), and these three are always called from an anonymous flow - login,
        // register's duplicate check, Google sign-in, forgot-password - where no tenant is known
        // yet. IgnoreQueryFilters() is required here or the tenant filter (comparing against a
        // null ambient TenantId) would silently match zero rows and every one of those flows
        // would appear to fail with "user not found".

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.GoogleId == googleId && x.IsActive);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Username == username && x.IsActive);
        }

        // Deliberately DOES keep the tenant filter: once a caller is authenticated and knows a
        // specific Id, lookups by Id are tenant-scoped (e.g. an Admin managing accounts should
        // only ever find users within their own tenant).
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Users.OrderBy(x => x.Username).AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}
