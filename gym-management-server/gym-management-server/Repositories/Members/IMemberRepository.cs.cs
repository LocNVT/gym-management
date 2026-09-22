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
    }
}
