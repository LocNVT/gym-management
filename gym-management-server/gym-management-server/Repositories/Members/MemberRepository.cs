using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Members
{
    public class MemberRepository : IMemberRepository
    {
        private readonly GymManagementContext _db;
        public MemberRepository(GymManagementContext db) => _db = db;

        public async Task AddAsync(Member member)
        {
            _db.Members.Add(member);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var member = await _db.Members.FindAsync(id);
            if (member != null)
            {
                member.IsDeleted = true; // soft delete
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Member>> GetAllAsync()
        {
            return await _db.Members.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task<(List<Member> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Members.Where(x => !x.IsDeleted);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<Member?> GetByIdAsync(Guid id)
        {
            var member = await _db.Members.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return member;
        }

        public async Task UpdateAsync(Member member)
        {
            _db.Members.Update(member);
            await _db.SaveChangesAsync();
        }

        public async Task<List<MemberRow>> GetForExportAsync() =>
            await _db.Members
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .Select(x => new MemberRow
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    DateOfBirth = x.DateOfBirth,
                    Gender = x.Gender,
                    Address = x.Address,
                    EmergencyName = x.EmergencyName,
                    EmergencyPhone = x.EmergencyPhone,
                    Status = x.Status,
                    Notes = x.Notes,
                    RegistrationDate = x.RegistrationDate,
                    CreatedAt = x.CreatedAt,
                })
                .Take(ExcelWriter.MaxRows + 1)
                .ToListAsync();

        public async Task<List<(string PhoneNumber, bool IsDeleted)>> GetAllPhoneNumbersWithDeletedStateAsync()
        {
            var raw = await _db.Members
                .Select(x => new { x.PhoneNumber, x.IsDeleted })
                .ToListAsync();
            return raw.Select(x => (x.PhoneNumber, x.IsDeleted)).ToList();
        }

        /// <summary>One SaveChanges for the whole batch, so the caller's transaction covers it.</summary>
        public async Task AddRangeAsync(IEnumerable<Member> members)
        {
            _db.Members.AddRange(members);
            await _db.SaveChangesAsync();
        }
    }
}
