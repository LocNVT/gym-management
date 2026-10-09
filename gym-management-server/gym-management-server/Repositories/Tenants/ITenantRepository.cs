using gym_management_server.Entities.Tenants;

namespace gym_management_server.Repositories.Tenants
{
    public interface ITenantRepository
    {
        Task AddAsync(Tenant tenant);
        Task<Tenant?> GetByIdAsync(Guid id);
    }
}
