using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.Members;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Bulk avatar import/export from docs/ImprovementPlan.md mục 3: a .zip of
    /// "{PhoneNumber}.{ext}" files, phone number because that's the identifier a staff member
    /// preparing the batch actually has on hand (not each member's internal Guid Id).
    /// </summary>
    public class MemberAvatarImportExportTests : IDisposable
    {
        private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), "gym-avatar-tests-" + Guid.NewGuid());

        public MemberAvatarImportExportTests() => Directory.CreateDirectory(_webRootPath);
        public void Dispose() { if (Directory.Exists(_webRootPath)) Directory.Delete(_webRootPath, recursive: true); }

        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static MemberService BuildService(GymManagementContext db) =>
            new(new MemberRepository(db), new GymManagementServiceMapObjects());

        private static readonly byte[] TinyPng =
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
        };

        private static byte[] BuildZip(params (string EntryName, byte[] Content)[] files)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (entryName, content) in files)
                {
                    var entry = archive.CreateEntry(entryName);
                    using var entryStream = entry.Open();
                    entryStream.Write(content);
                }
            }
            return stream.ToArray();
        }

        [Fact]
        public async Task Import_matches_by_phone_number_and_saves_the_avatar()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Ảnh Hội Viên", PhoneNumber = "0977111222" };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            var service = BuildService(db);
            var zip = BuildZip(("0977111222.png", TinyPng));

            var report = await service.ImportAvatarsAsync(new MemoryStream(zip), _webRootPath);

            Assert.Equal(1, report.SuccessCount);
            Assert.Equal(0, report.FailureCount);

            var stored = await db.Members.SingleAsync();
            Assert.NotNull(stored.AvatarUrl);
            Assert.True(File.Exists(Path.Combine(_webRootPath, stored.AvatarUrl!.TrimStart('/'))));
        }

        [Fact]
        public async Task Import_reports_an_unknown_phone_number_without_touching_other_entries()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Known Member", PhoneNumber = "0977111222" };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            var service = BuildService(db);
            var zip = BuildZip(
                ("0977111222.png", TinyPng),
                ("0900000000.png", TinyPng)); // no such member

            var report = await service.ImportAvatarsAsync(new MemoryStream(zip), _webRootPath);

            Assert.Equal(1, report.SuccessCount);
            Assert.Equal(1, report.FailureCount);
            var failure = report.Results.Single(r => !r.Success);
            Assert.Equal("0900000000", failure.PhoneNumber);
            Assert.Contains("Không tìm thấy hội viên", failure.Message);
        }

        [Fact]
        public async Task Import_rejects_a_file_whose_content_does_not_match_its_extension()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Spoofed Upload", PhoneNumber = "0977111222" };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            var service = BuildService(db);
            var fakeImage = System.Text.Encoding.ASCII.GetBytes("not actually a png");
            var zip = BuildZip(("0977111222.png", fakeImage));

            var report = await service.ImportAvatarsAsync(new MemoryStream(zip), _webRootPath);

            Assert.Equal(0, report.SuccessCount);
            Assert.Equal(1, report.FailureCount);

            var stored = await db.Members.SingleAsync();
            Assert.Null(stored.AvatarUrl);
        }

        [Fact]
        public async Task Export_zips_every_members_avatar_named_by_phone_number()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var member = new Member { Id = Guid.NewGuid(), FullName = "Export Me", PhoneNumber = "0977111222" };
            db.Members.Add(member);
            await db.SaveChangesAsync();
            await service.ImportAvatarsAsync(new MemoryStream(BuildZip(("0977111222.png", TinyPng))), _webRootPath);

            var zipBytes = await service.ExportAvatarsAsync(_webRootPath);

            using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            var entry = Assert.Single(archive.Entries);
            Assert.Equal("0977111222.png", entry.Name);
        }

        [Fact]
        public async Task Export_skips_members_without_an_avatar()
        {
            using var db = NewContext();
            db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "No Photo", PhoneNumber = "0988111222" });
            await db.SaveChangesAsync();

            var service = BuildService(db);
            var zipBytes = await service.ExportAvatarsAsync(_webRootPath);

            using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            Assert.Empty(archive.Entries);
        }
    }
}
