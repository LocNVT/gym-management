using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ImportApiTests : IClassFixture<SqliteWebApplicationFactory>
    {
        private readonly SqliteWebApplicationFactory _factory;
        public ImportApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

        private static MultipartFormDataContent FileWith(params string[][] dataRows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Thành viên");
            var headers = new[] { "Họ và tên", "Số điện thoại", "Trạng thái" };
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

        private int MemberCount()
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<GymManagementContext>().Members.Count();
        }

        [Fact]
        public async Task Import_requires_authentication()
        {
            var response = await _factory.CreateClient()
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["A", "0900000001", "Hoạt động"]));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Template_endpoint_returns_a_workbook_with_the_import_headers()
        {
            var response = await _factory.CreateAuthenticatedClient(0).GetAsync("/api/Member/import/template");
            response.EnsureSuccessStatusCode();

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.StartsWith("Họ và tên", wb.Worksheet("Thành viên").Cell(1, 1).GetString());
        }

        [Fact]
        public async Task A_dry_run_writes_nothing_even_when_the_file_is_perfect()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Nguyễn Văn A", "0900000011", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("isClean").GetBoolean());
            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, MemberCount());
        }

        [Fact]
        public async Task A_committed_run_writes_the_rows()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=false",
                    FileWith(["Nguyễn Văn B", "0900000021", "Hoạt động"],
                             ["Trần Thị C", "0900000022", "Tạm ngưng"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("committed").GetBoolean());
            Assert.Equal(2, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before + 2, MemberCount());
        }

        [Fact]
        public async Task One_bad_row_stops_the_whole_file()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=false",
                    FileWith(["Hợp lệ", "0900000031", "Hoạt động"],
                             ["Sai trạng thái", "0900000032", "Đang nghỉ"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, MemberCount());   // the valid row must NOT have been written

            var error = body.GetProperty("errors")[0];
            Assert.Equal(3, error.GetProperty("rowNumber").GetInt32());
            Assert.Contains("Hoạt động", error.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_phone_number_already_in_the_database_is_rejected()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Đã có", PhoneNumber = "0900000041" });
                db.SaveChanges();
            }

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Trùng", "0900000041", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Contains("đã tồn tại", body.GetProperty("errors")[0].GetProperty("message").GetString());
        }

        [Fact]
        public async Task Two_rows_sharing_a_phone_number_are_rejected_before_the_database_sees_them()
        {
            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true",
                    FileWith(["Một", "0900000051", "Hoạt động"],
                             ["Hai", "0900000051", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var message = body.GetProperty("errors")[0].GetProperty("message").GetString();
            Assert.Contains("trùng với dòng 2", message);
        }

        // Handed over from the previous task: MemberImportService.FindDuplicatesAsync has two
        // branches for a colliding phone number — one for a soft-deleted member, one for an
        // active one — and the two must never cross-fire.

        [Fact]
        public async Task A_phone_belonging_to_a_soft_deleted_member_gets_the_deleted_specific_message()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                db.Members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    FullName = "Đã xóa",
                    PhoneNumber = "0900000061",
                    IsDeleted = true,
                });
                db.SaveChanges();
            }

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Trùng", "0900000061", "Hoạt động"]));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var message = body.GetProperty("errors")[0].GetProperty("message").GetString();
            Assert.Contains("đã bị xóa", message);
            Assert.DoesNotContain("đã tồn tại", message);
        }

        [Fact]
        public async Task An_active_duplicate_phone_gets_the_plain_message_not_the_deleted_one()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                db.Members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    FullName = "Đang hoạt động",
                    PhoneNumber = "0900000062",
                    IsDeleted = false,
                });
                db.SaveChanges();
            }

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Trùng", "0900000062", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var message = body.GetProperty("errors")[0].GetProperty("message").GetString();
            Assert.Contains("đã tồn tại", message);
            Assert.DoesNotContain("đã bị xóa", message);
        }
    }
}
