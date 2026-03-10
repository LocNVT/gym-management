using gym_management_server.Entities.CheckIns;
using System.Linq.Expressions;

namespace gym_management_server.Repositories.CheckIns
{
    public interface ICheckInRepository
    {
        Task<CheckIn?> GetByIdAsync(Guid id);
        Task<List<CheckIn>> GetAllAsync(Expression<Func<CheckIn, bool>>? predicate = null);
        Task<(List<CheckIn> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(CheckIn checkIn);
        Task UpdateAsync(CheckIn checkIn);
        Task DeleteAsync(Guid id);
    }
}
