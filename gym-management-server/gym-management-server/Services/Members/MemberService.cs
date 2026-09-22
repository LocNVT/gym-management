using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Members;

namespace gym_management_server.Services.Members
{
    public class MemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public MemberService(IMemberRepository memberRepository, GymManagementServiceMapObjects mapObjects)
        {
            _memberRepository = memberRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<MemberOutput>> GetAllMembersAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _memberRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(m => _mapObjects.MapObjects<Member, MemberOutput>(m)).ToList();
            return new PagedResult<MemberOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<MemberOutput?> GetMemberByIdAsync(Guid id)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            return _mapObjects.MapObjects<Member, MemberOutput>(member);
        }

        public async Task<MemberOutput> CreateMemberAsync(MemberInput input)
        {
            var member = new Member();
            member.Id = Guid.NewGuid();
            member.FullName = input.FullName;
            member.Address = input.Address;
            member.PhoneNumber = input.PhoneNumber;
            member.DateOfBirth = input.DateOfBirth;
            member.Status = input.Status;
            member.AvatarUrl = input.AvatarUrl;
            member.Email = input.Email;
            member.EmergencyName = input.EmergencyName;
            member.EmergencyPhone = input.EmergencyPhone;

            await _memberRepository.AddAsync(member);
            return _mapObjects.MapObjects<Member, MemberOutput>(member);
        }

        public async Task<MemberOutput?> UpdateMemberAsync(Guid id, MemberInput input)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return null;

            member.FullName = input.FullName;
            member.Address = input.Address;
            member.PhoneNumber = input.PhoneNumber;
            member.DateOfBirth = input.DateOfBirth;
            member.Status = input.Status;
            member.AvatarUrl = input.AvatarUrl;
            member.Email = input.Email;
            member.EmergencyName = input.EmergencyName;
            member.EmergencyPhone = input.EmergencyPhone;

            await _memberRepository.UpdateAsync(member);
            return _mapObjects.MapObjects<Member, MemberOutput>(member);
        }

        public async Task<bool> DeleteMemberAsync(Guid id)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return false;

            await _memberRepository.DeleteAsync(id);
            return true;
        }

        public async Task<string?> UploadAvatarAsync(Guid id, IFormFile file, string webRootPath)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return null;

            var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(member.AvatarUrl))
            {
                var oldPath = Path.Combine(webRootPath, member.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{id}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            member.AvatarUrl = $"/uploads/avatars/{fileName}";
            member.UpdatedAt = DateTime.UtcNow;
            await _memberRepository.UpdateAsync(member);

            return member.AvatarUrl;
        }

        public async Task<string?> GetAvatarPathAsync(Guid id, string webRootPath)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null || string.IsNullOrEmpty(member.AvatarUrl)) return null;

            var filePath = Path.Combine(webRootPath, member.AvatarUrl.TrimStart('/'));
            return File.Exists(filePath) ? filePath : null;
        }

        public async Task<byte[]> ExportAsync() =>
            ExcelWriter.Write(MemberSheet.Export, await _memberRepository.GetForExportAsync());
    }
}
