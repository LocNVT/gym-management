using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Members;

namespace gym_management_server.Repositories.Members
{
    public interface IMemberRepository
    {
        Task<Member?> GetByIdAsync(Guid id);
        Task<List<Member>> GetAllAsync();
        Task<(List<Member> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(Member member);
        Task UpdateAsync(Member member);
        Task DeleteAsync(Guid id);

        /// <summary>All non-deleted members, ordered by name, projected for Excel export.</summary>
        Task<List<MemberRow>> GetForExportAsync();

        /// <summary>Phone numbers of every non-deleted member, for import duplicate checks.</summary>
        Task<List<string>> GetAllPhoneNumbersAsync();

        /// <summary>Adds every member and issues a single SaveChanges for the whole batch,
        /// so the caller's transaction covers it.</summary>
        Task AddRangeAsync(IEnumerable<Member> members);
    }
}
