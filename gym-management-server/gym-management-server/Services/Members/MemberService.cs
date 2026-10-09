using System.IO.Compression;
using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Infrastructure.Images;
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
            member.Gender = input.Gender;
            member.Notes = input.Notes;

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
            member.Gender = input.Gender;
            member.Notes = input.Notes;

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

            await using var stream = file.OpenReadStream();
            return await SaveAvatarAsync(member, stream, Path.GetExtension(file.FileName), webRootPath);
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

        /// <summary>
        /// Bulk-imports avatars from a .zip whose entries are named "{PhoneNumber}.{ext}" - phone
        /// number because that's the one identifier a staff member preparing the batch actually
        /// knows, unlike each member's internal Guid Id. See docs/ImprovementPlan.md mục 3.
        /// </summary>
        public async Task<AvatarImportReport> ImportAvatarsAsync(Stream zipStream, string webRootPath)
        {
            var results = new List<AvatarImportResultOutput>();

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry
                    results.Add(await ImportAvatarEntryAsync(entry, webRootPath));
                }
            }

            return new AvatarImportReport
            {
                Results = results,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
            };
        }

        /// <summary>Zips every current avatar, named "{PhoneNumber}.{ext}" to match the import convention.</summary>
        public async Task<byte[]> ExportAvatarsAsync(string webRootPath)
        {
            var members = await _memberRepository.GetWithAvatarAsync();

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var member in members)
                {
                    var filePath = Path.Combine(webRootPath, member.AvatarUrl!.TrimStart('/'));
                    if (!File.Exists(filePath)) continue; // DB/disk drifted - skip rather than fail the whole export

                    var entry = archive.CreateEntry($"{member.PhoneNumber}{Path.GetExtension(filePath)}", CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(filePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            return zipStream.ToArray();
        }

        private async Task<AvatarImportResultOutput> ImportAvatarEntryAsync(ZipArchiveEntry entry, string webRootPath)
        {
            var phoneNumber = Path.GetFileNameWithoutExtension(entry.Name);
            var extension = Path.GetExtension(entry.Name).ToLowerInvariant();

            if (!ImageFileValidator.AllowedExtensions.Contains(extension))
                return AvatarImportResultOutput.Fail(entry.Name, phoneNumber, $"Định dạng '{extension}' không được hỗ trợ.");
            if (entry.Length > ImageFileValidator.MaxBytes)
                return AvatarImportResultOutput.Fail(entry.Name, phoneNumber, "File vượt quá 5 MB.");

            var member = await _memberRepository.GetByPhoneNumberAsync(phoneNumber);
            if (member == null)
                return AvatarImportResultOutput.Fail(entry.Name, phoneNumber, $"Không tìm thấy hội viên với số điện thoại '{phoneNumber}'.");

            using var buffer = new MemoryStream();
            await using (var entryStream = entry.Open())
                await entryStream.CopyToAsync(buffer);

            if (!ImageFileValidator.LooksLikeImage(buffer.ToArray(), extension))
                return AvatarImportResultOutput.Fail(entry.Name, phoneNumber, "Nội dung file không khớp với định dạng ảnh đã khai báo.");

            buffer.Position = 0;
            await SaveAvatarAsync(member, buffer, extension, webRootPath);

            return AvatarImportResultOutput.Ok(entry.Name, phoneNumber);
        }

        private async Task<string> SaveAvatarAsync(Member member, Stream content, string extension, string webRootPath)
        {
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(member.AvatarUrl))
            {
                var oldPath = Path.Combine(webRootPath, member.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var fileName = $"{member.Id}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await content.CopyToAsync(stream);
            }

            member.AvatarUrl = $"/uploads/avatars/{fileName}";
            member.UpdatedAt = DateTime.UtcNow;
            await _memberRepository.UpdateAsync(member);

            return member.AvatarUrl;
        }
    }
}
