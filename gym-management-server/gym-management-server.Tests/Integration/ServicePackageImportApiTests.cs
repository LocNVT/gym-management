using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.ServicePackages;
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
            return scope.ServiceProvider.GetRequiredService<GymManagementContext>().ServicePackages.Count();
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
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
            Assert.True(db.ServicePackages.Single(p => p.Name == nameA).IsActive);
            Assert.True(db.ServicePackages.Single(p => p.Name == nameB).IsActive);
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
                db.ServicePackages.Add(new ServicePackage { Id = Guid.NewGuid(), Name = name, DurationDays = 30 });
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
    }
}
