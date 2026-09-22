using gym_management_server.DTOs.Export;
using gym_management_server.Entities.MemberDataServices;
using System.Linq.Expressions;

namespace gym_management_server.Repositories.MemberDataServices
{
    public interface IMemberDataServiceRepository
    {
        Task<MemberDataService?> GetByIdAsync(Guid id);
        Task<List<MemberDataService>> GetAllAsync(Expression<Func<MemberDataService, bool>>? predicate = null);
        Task<(List<MemberDataService> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(MemberDataService MemberDataService);
        Task UpdateAsync(MemberDataService MemberDataService);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Subscriptions starting within [from, to] (inclusive of the whole `to` day), newest first,
        /// with the member's and package's names resolved via their navigations, for Excel export.
        /// </summary>
        Task<List<SubscriptionRow>> GetForExportAsync(DateTime? from, DateTime? to);
    }
}
