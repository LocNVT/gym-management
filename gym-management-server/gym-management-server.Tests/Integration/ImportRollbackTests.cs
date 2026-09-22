using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Repositories.Members;
using gym_management_server.Repositories.ServicePackages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Handed over from the previous task: both import services catch <see cref="DbUpdateException"/>
    /// around the commit to cover a race between the pre-commit duplicate check and the actual
    /// write (a concurrent import landing in between), rolling back via the ambient
    /// <c>BeginTransactionAsync</c> transaction. Neither Member.PhoneNumber's real unique index
    /// nor (especially) ServicePackage.Name, which has no unique index at all, can be coaxed
    /// into throwing that exception from a single-threaded test, so these tests swap in a
    /// repository that genuinely writes the rows through the real, SQLite-backed implementation
    /// first — so they land for real inside the service's ambient transaction — and only then
    /// throws DbUpdateException. This is what lets the test discriminate a real rollback from a
    /// row that was simply never written: if the write happened first and the count is still
    /// zero afterwards, the transaction's rollback actually undid it. (Verified by temporarily
    /// deleting the transaction from MemberImportService: with no transaction to roll back,
    /// SaveChangesAsync inside AddRangeAsync auto-commits immediately, and this test fails with
    /// count == 1. See task-4-report.md for the recorded outcome.)
    /// </summary>
    internal sealed class ThrowingMemberRepository : IMemberRepository
    {
        private readonly MemberRepository _inner;
        public ThrowingMemberRepository(GymManagementContext db) => _inner = new MemberRepository(db);

        public Task<Member?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);
        public Task<List<Member>> GetAllAsync() => _inner.GetAllAsync();
        public Task<(List<Member> Items, int TotalCount)> GetPagedAsync(int page, int pageSize) => _inner.GetPagedAsync(page, pageSize);
        public Task AddAsync(Member member) => _inner.AddAsync(member);
        public Task UpdateAsync(Member member) => _inner.UpdateAsync(member);
        public Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);
        public Task<List<MemberRow>> GetForExportAsync() => _inner.GetForExportAsync();
        public Task<List<(string PhoneNumber, bool IsDeleted)>> GetAllPhoneNumbersWithDeletedStateAsync() =>
            _inner.GetAllPhoneNumbersWithDeletedStateAsync();

        public async Task AddRangeAsync(IEnumerable<Member> members)
        {
            // Write for real first -- inside whatever ambient transaction the caller started --
            // then simulate the race. If the caller's transaction is ever removed, this write
            // auto-commits right here and no rollback can undo it.
            await _inner.AddRangeAsync(members);
            throw new DbUpdateException("Simulated: a concurrent import committed between the duplicate check and this commit.");
        }
    }

    internal sealed class ThrowingServicePackageRepository : IServicePackageRepository
    {
        private readonly ServicePackageRepository _inner;
        public ThrowingServicePackageRepository(GymManagementContext db) => _inner = new ServicePackageRepository(db);

        public Task<ServicePackage?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);
        public Task<List<ServicePackage>> GetAllAsync() => _inner.GetAllAsync();
        public Task<(List<ServicePackage> Items, int TotalCount)> GetPagedAsync(int page, int pageSize) => _inner.GetPagedAsync(page, pageSize);
        public Task AddAsync(ServicePackage servicePackage) => _inner.AddAsync(servicePackage);
        public Task UpdateAsync(ServicePackage servicePackage) => _inner.UpdateAsync(servicePackage);
        public Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);
        public Task<List<ServicePackageRow>> GetForExportAsync() => _inner.GetForExportAsync();
        public Task<List<string>> GetAllNamesAsync() => _inner.GetAllNamesAsync();

        public async Task AddRangeAsync(IEnumerable<ServicePackage> packages)
        {
            // Write for real first -- inside whatever ambient transaction the caller started --
            // then simulate the race. If the caller's transaction is ever removed, this write
            // auto-commits right here and no rollback can undo it.
            await _inner.AddRangeAsync(packages);
            throw new DbUpdateException("Simulated: a concurrent import committed between the duplicate check and this commit.");
        }
    }

    public class ThrowingMemberRepositoryFactory : SqliteWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMemberRepository>();
                services.AddScoped<IMemberRepository, ThrowingMemberRepository>();
            });
        }
    }

    public class ThrowingServicePackageRepositoryFactory : SqliteWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IServicePackageRepository>();
                services.AddScoped<IServicePackageRepository, ThrowingServicePackageRepository>();
            });
        }
    }

    public class MemberImportRollbackTests : IClassFixture<ThrowingMemberRepositoryFactory>
    {
        private readonly ThrowingMemberRepositoryFactory _factory;
        public MemberImportRollbackTests(ThrowingMemberRepositoryFactory factory) => _factory = factory;

        private static MultipartFormDataContent MemberFile(string fullName, string phone)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Thành viên");
            ws.Cell(1, 1).Value = "Họ và tên";
            ws.Cell(1, 2).Value = "Số điện thoại";
            ws.Cell(1, 3).Value = "Trạng thái";
            ws.Cell(2, 1).Value = fullName;
            ws.Cell(2, 2).Value = phone;
            ws.Cell(2, 3).Value = "Hoạt động";

            var stream = new MemoryStream();
            wb.SaveAs(stream);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(stream.ToArray()), "file", "import.xlsx");
            return content;
        }

        [Fact]
        public async Task A_DbUpdateException_after_a_real_write_is_genuinely_rolled_back()
        {
            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=false", MemberFile("Người mới", "0900000071"));
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());

            var errors = body.GetProperty("errors");
            Assert.Equal(1, errors.GetArrayLength());
            var error = errors[0];
            Assert.False(error.TryGetProperty("columnHeader", out var col) && col.ValueKind != JsonValueKind.Null);
            Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));

            // The decorator wrote this row for real before throwing. If it is still here, the
            // service's transaction did not actually roll back -- this is the assertion that
            // discriminates a real rollback from one that just never wrote anything.
            using var scope = _factory.Services.CreateScope();
            var count = scope.ServiceProvider.GetRequiredService<GymManagementContext>().Members
                .Count(m => m.PhoneNumber == "0900000071");
            Assert.Equal(0, count);
        }
    }

    public class ServicePackageImportRollbackTests : IClassFixture<ThrowingServicePackageRepositoryFactory>
    {
        private readonly ThrowingServicePackageRepositoryFactory _factory;
        public ServicePackageImportRollbackTests(ThrowingServicePackageRepositoryFactory factory) => _factory = factory;

        private static MultipartFormDataContent PackageFile(string name)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Gói dịch vụ");
            ws.Cell(1, 1).Value = "Tên gói";
            ws.Cell(2, 1).Value = name;

            var stream = new MemoryStream();
            wb.SaveAs(stream);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(stream.ToArray()), "file", "import.xlsx");
            return content;
        }

        [Fact]
        public async Task A_DbUpdateException_after_a_real_write_is_genuinely_rolled_back()
        {
            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=false", PackageFile("Gói mới " + Guid.NewGuid()));
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());

            var errors = body.GetProperty("errors");
            Assert.Equal(1, errors.GetArrayLength());
            Assert.False(string.IsNullOrWhiteSpace(errors[0].GetProperty("message").GetString()));

            // The decorator wrote this row for real before throwing. If it is still here, the
            // service's transaction did not actually roll back -- this is the assertion that
            // discriminates a real rollback from one that just never wrote anything. (This
            // factory's database is private to this test class, so counting the whole table is
            // safe.)
            using var scope = _factory.Services.CreateScope();
            var count = scope.ServiceProvider.GetRequiredService<GymManagementContext>().ServicePackages.Count();
            Assert.Equal(0, count);
        }
    }
}
