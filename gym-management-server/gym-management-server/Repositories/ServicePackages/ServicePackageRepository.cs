using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.ServicePackages;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.ServicePackages
{
    public class ServicePackageRepository : IServicePackageRepository
    {
        private readonly GymManagementContext _db;

        public ServicePackageRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ServicePackage servicePackage)
        {
            _db.ServicePackages.Add(servicePackage);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var servicePackage = await _db.ServicePackages.FirstOrDefaultAsync(x => x.Id == id);
            if (servicePackage != null)
            {
                _db.Remove(servicePackage);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<ServicePackage>> GetAllAsync()
        {
            return await _db.ServicePackages.ToListAsync();
        }

        public async Task<(List<ServicePackage> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.ServicePackages.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<ServicePackage?> GetByIdAsync(Guid id)
        {
            return await _db.ServicePackages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(ServicePackage servicePackage)
        {
            _db.ServicePackages.Update(servicePackage);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ServicePackageRow>> GetForExportAsync() =>
            await _db.ServicePackages
                .OrderBy(x => x.Name)
                .Select(x => new ServicePackageRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Price = x.Price,
                    DurationDays = x.DurationDays,
                    MaxCheckins = x.MaxCheckins,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync();
    }
}
