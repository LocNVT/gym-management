using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.ServicePackages;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Repositories.ServicePackages;

namespace gym_management_server.Services.ServicePackages
{
    public class ServicePackageService
    {
        private readonly IServicePackageRepository _servicePackageRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public ServicePackageService(IServicePackageRepository servicePackageRepository, GymManagementServiceMapObjects mapObjects)
        {
            _servicePackageRepository = servicePackageRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<ServicePackageOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _servicePackageRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(s => _mapObjects.MapObjects<ServicePackage, ServicePackageOutput>(s)).ToList();
            return new PagedResult<ServicePackageOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<ServicePackageOutput?> GetByIdAsync(Guid id)
        {
            var servicePackage = await _servicePackageRepository.GetByIdAsync(id);
            if (servicePackage == null) return null;

            return _mapObjects.MapObjects<ServicePackage, ServicePackageOutput>(servicePackage);
        }

        public async Task<ServicePackageOutput> CreateAsync(ServicePackageInput input)
        {
            var servicePackage = new ServicePackage();
            servicePackage.Id = Guid.NewGuid();
            servicePackage.Name = input.Name;
            servicePackage.Description = input.Description;
            servicePackage.Price = input.Price;
            servicePackage.DurationDays = input.DurationDays;
            servicePackage.MaxCheckins = input.MaxCheckins;
            servicePackage.IsActive = input.IsActive;
            servicePackage.CreatedAt = DateTime.UtcNow;

            await _servicePackageRepository.AddAsync(servicePackage);
            return _mapObjects.MapObjects<ServicePackage, ServicePackageOutput>(servicePackage);
        }

        public async Task<ServicePackageOutput?> UpdateAsync(Guid id, ServicePackageInput input)
        {
            var servicePackage = await _servicePackageRepository.GetByIdAsync(id);
            if (servicePackage == null) return null;

            servicePackage.Name = input.Name;
            servicePackage.Description = input.Description;
            servicePackage.Price = input.Price;
            servicePackage.DurationDays = input.DurationDays;
            servicePackage.MaxCheckins = input.MaxCheckins;
            servicePackage.IsActive = input.IsActive;
            servicePackage.UpdatedAt = DateTime.UtcNow;

            await _servicePackageRepository.UpdateAsync(servicePackage);
            return _mapObjects.MapObjects<ServicePackage, ServicePackageOutput>(servicePackage);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var servicePackage = await _servicePackageRepository.GetByIdAsync(id);
            if (servicePackage == null) return false;

            await _servicePackageRepository.DeleteAsync(id);
            return true;
        }
    }
}
