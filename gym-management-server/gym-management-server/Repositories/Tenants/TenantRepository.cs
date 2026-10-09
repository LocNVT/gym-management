using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Tenants;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Tenants
{
    public class TenantRepository : ITenantRepository
    {
        private readonly GymManagementContext _db;

        public TenantRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Tenant tenant)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }

        public async Task<Tenant?> GetByIdAsync(Guid id)
        {
            return await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
