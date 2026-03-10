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
    }
}
