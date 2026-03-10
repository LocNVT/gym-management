using gym_management_server.Entities.ServicePackages;

namespace gym_management_server.Repositories.ServicePackages
{
    public interface IServicePackageRepository
    {
        Task<ServicePackage?> GetByIdAsync(Guid id);
        Task<List<ServicePackage>> GetAllAsync();
        Task<(List<ServicePackage> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(ServicePackage servicePackage);
        Task UpdateAsync(ServicePackage servicePackage);
        Task DeleteAsync(Guid id);
    }
}
