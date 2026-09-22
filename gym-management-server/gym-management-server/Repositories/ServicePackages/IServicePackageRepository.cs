using gym_management_server.DTOs.Export;
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

        /// <summary>All service packages, ordered by name, projected for Excel export.</summary>
        Task<List<ServicePackageRow>> GetForExportAsync();

        /// <summary>Names of every service package, for import duplicate checks.</summary>
        Task<List<string>> GetAllNamesAsync();

        /// <summary>Adds every package and issues a single SaveChanges for the whole batch,
        /// so the caller's transaction covers it.</summary>
        Task AddRangeAsync(IEnumerable<ServicePackage> packages);
    }
}
