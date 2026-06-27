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

        /// <summary>The member's currently-open attendance session (CheckOutTime == null), if any.</summary>
        Task<CheckIn?> GetActiveByMemberAsync(Guid memberId);

        /// <summary>Paged attendance history for a member, most recent first.</summary>
        Task<(List<CheckIn> Items, int TotalCount)> GetByMemberPagedAsync(Guid memberId, int page, int pageSize);

        /// <summary>All currently-open sessions (CheckOutTime == null), with Member loaded, oldest first.</summary>
        Task<List<CheckIn>> GetActiveSessionsAsync();

        /// <summary>Count of sessions started within [from, to).</summary>
        Task<int> CountCheckInsBetweenAsync(DateTime from, DateTime to);

        /// <summary>Count of sessions checked out within [from, to).</summary>
        Task<int> CountCheckOutsBetweenAsync(DateTime from, DateTime to);

        /// <summary>Count of sessions currently open.</summary>
        Task<int> CountCurrentlyInsideAsync();

        /// <summary>Most recent sessions across all members, with Member loaded.</summary>
        Task<List<CheckIn>> GetRecentAsync(int count);
    }
}
