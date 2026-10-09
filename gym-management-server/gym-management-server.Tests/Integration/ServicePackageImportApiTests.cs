using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.ServicePackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Equivalent of <see cref="ImportApiTests"/> for the ServicePackage import endpoint. The
    /// final whole-branch review found that only the Member side had API-level coverage despite
    /// the controller code being duplicated by hand for ServicePackage -- so a regression in the
    /// copy (a missing [Authorize], a route typo, a duplicated bug in the hand-copied validation)
    /// had nothing to catch it.
    /// </summary>
    public class ServicePackageImportApiTests : IClassFixture<SqliteWebApplicationFactory>
    {
        private readonly SqliteWebApplicationFactory _factory;
        public ServicePackageImportApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

        private static MultipartFormDataContent FileWith(params string[][] dataRows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Gói dịch vụ");
            var headers = new[] { "Tên gói", "Số ngày" };
            for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            for (var r = 0; r < dataRows.Length; r++)
                for (var c = 0; c < dataRows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = dataRows[r][c];

            var stream = new MemoryStream();
            wb.SaveAs(stream);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(stream.ToArray()), "file", "import.xlsx");
            return content;
        }

        private int PackageCount()
        {
            using var scope = _factory.Services.CreateScope();
            // IgnoreQueryFilters: see ImportApiTests.MemberCount for why.
            return scope.ServiceProvider.GetRequiredService<GymManagementContext>().ServicePackages.IgnoreQueryFilters().Count();
        }

        [Fact]
        public async Task Import_requires_authentication()
        {
            var response = await _factory.CreateClient()
                .PostAsync("/api/ServicePackage/import?dryRun=true", FileWith(["Gói A", "30"]));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Template_endpoint_returns_a_workbook_with_the_import_headers()
        {
            var response = await _factory.CreateAuthenticatedClient(0).GetAsync("/api/ServicePackage/import/template");
            response.EnsureSuccessStatusCode();

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.StartsWith("Tên gói", wb.Worksheet("Gói dịch vụ").Cell(1, 1).GetString());
        }

        [Fact]
        public async Task A_dry_run_writes_nothing_even_when_the_file_is_perfect()
        {
            var before = PackageCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true",
                    FileWith(["Gói dry-run " + Guid.NewGuid(), "30"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("isClean").GetBoolean());
            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, PackageCount());
        }

        [Fact]
        public async Task A_committed_run_writes_the_rows()
        {
            var before = PackageCount();
            var nameA = "Gói commit A " + Guid.NewGuid();
            var nameB = "Gói commit B " + Guid.NewGuid();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=false",
                    FileWith([nameA, "30"], [nameB, "90"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("committed").GetBoolean());
            Assert.Equal(2, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before + 2, PackageCount());

            // Item 5's default fix: a package imported with the "Đang áp dụng" column left
            // blank must come in active, not silently disabled.
            // IgnoreQueryFilters: see PackageCount for why (this verification runs outside any
            // HTTP request, so there is no JWT for the tenant filter to read).
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
            Assert.True(db.ServicePackages.IgnoreQueryFilters().Single(p => p.Name == nameA).IsActive);
            Assert.True(db.ServicePackages.IgnoreQueryFilters().Single(p => p.Name == nameB).IsActive);
        }

        [Fact]
        public async Task One_bad_row_stops_the_whole_file()
        {
            var before = PackageCount();
            var validName = "Gói hợp lệ " + Guid.NewGuid();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=false",
                    FileWith([validName, "30"], ["Gói thiếu số ngày " + Guid.NewGuid(), ""]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, PackageCount());   // the valid row must NOT have been written

            var error = body.GetProperty("errors")[0];
            Assert.Equal(3, error.GetProperty("rowNumber").GetInt32());
            Assert.Contains("Số ngày", error.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_duplicate_name_against_the_database_is_rejected()
        {
            var name = "Gói trùng tên " + Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                db.ServicePackages.Add(new ServicePackage { Id = Guid.NewGuid(), TenantId = SqliteWebApplicationFactory.TestTenantId, Name = name, DurationDays = 30 });
                db.SaveChanges();
            }

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", FileWith([name, "30"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Contains("đã tồn tại", body.GetProperty("errors")[0].GetProperty("message").GetString());
        }

        [Fact]
        public async Task Two_rows_sharing_a_name_are_rejected_before_the_database_sees_them()
        {
            var name = "Gói trùng trong file " + Guid.NewGuid();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", FileWith([name, "30"], [name, "60"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var message = body.GetProperty("errors")[0].GetProperty("message").GetString();
            Assert.Contains("trùng với dòng 2", message);
        }

        // Upload guards -- these four are hand-duplicated line-for-line in
        // ServicePackageController.Import from MemberController.Import (see ImportApiTests'
        // equivalents), so a regression in the copy needs its own test on the copy rather than
        // relying on the Member side's coverage.

        [Fact]
        public async Task An_upload_over_5MB_is_rejected_with_a_400()
        {
            var oversized = new byte[5 * 1024 * 1024 + 1];
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(oversized), "file", "import.xlsx");

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("5 MB", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_non_xlsx_upload_is_rejected_with_a_400()
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4 }), "file", "import.txt");

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(".xlsx", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_request_with_no_file_part_is_rejected_with_a_400()
        {
            // A MultipartFormDataContent with literally zero parts serializes to a body the
            // server's own form reader rejects before routing even runs ("invalid
            // Content-Disposition value"), which isn't the case under test here. Include an
            // unrelated field so the request is a well-formed multipart upload that simply
            // omits "file".
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("x"), "note");

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Chưa chọn file", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_corrupt_but_zip_shaped_upload_is_rejected_with_a_400_not_a_500()
        {
            var stream = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(
                       stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                // Deliberately no entries: a valid, empty ZIP archive, which is not a valid
                // OOXML workbook.
            }

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(stream.ToArray()), "file", "import.xlsx");

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/ServicePackage/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = body.GetProperty("message").GetString();
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.Contains("Excel", message);
        }
    }
}
