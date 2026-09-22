using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.MemberDataServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace gym_management_server.Repositories.MemberDataServices
{
    public class MemberDataServiceRepository : IMemberDataServiceRepository
    {
        private readonly GymManagementContext _db;

        public MemberDataServiceRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(MemberDataService MemberDataService)
        {
            _db.MemberDataServices.Add(MemberDataService);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var memberDataService = await _db.MemberDataServices.FirstOrDefaultAsync(x => x.Id == id);
            if (memberDataService != null)
            {
                _db.Remove(memberDataService);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<MemberDataService>> GetAllAsync(Expression<Func<MemberDataService, bool>>? predicate = null)
        {
            IQueryable<MemberDataService> query = _db.MemberDataServices;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync();
        }

        public async Task<MemberDataService?> GetByIdAsync(Guid id)
        {
            var memberDataService = await _db.MemberDataServices.FirstOrDefaultAsync(x => x.Id == id);
            return memberDataService;
        }

        public async Task<(List<MemberDataService> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.MemberDataServices.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task UpdateAsync(MemberDataService MemberDataService)
        {
            _db.MemberDataServices.Update(MemberDataService);
            await _db.SaveChangesAsync();
        }

        public async Task<List<SubscriptionRow>> GetForExportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.MemberDataServices.AsQueryable();
            if (from.HasValue) query = query.Where(x => x.StartDate >= from.Value.Date);
            // `to` is inclusive of the whole day the caller picked.
            if (to.HasValue) query = query.Where(x => x.StartDate < to.Value.Date.AddDays(1));

            return await query
                .OrderByDescending(x => x.StartDate)
                .Select(x => new SubscriptionRow
                {
                    Id = x.Id,
                    MemberName = x.Member.FullName,
                    MemberPhone = x.Member.PhoneNumber,
                    PackageName = x.ServicePackage.Name,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    PriceAtPurchase = x.PriceAtPurchase,
                    RemainingCheckins = x.RemainingCheckins,
                    Status = x.Status,
                })
                .ToListAsync();
        }
    }
}
