using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using Microsoft.EntityFrameworkCore;
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
            // IgnoreQueryFilters: this count runs outside any HTTP request, so there is no JWT for
            // the tenant query filter to read - without this it would always return 0 regardless
            // of what the (correctly tenant-scoped) import actually wrote.
            return scope.ServiceProvider.GetRequiredService<GymManagementContext>().Members.IgnoreQueryFilters().Count();
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
                db.Members.Add(new Member { Id = Guid.NewGuid(), TenantId = SqliteWebApplicationFactory.TestTenantId, FullName = "Đã có", PhoneNumber = "0900000041" });
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
                    TenantId = SqliteWebApplicationFactory.TestTenantId,
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
                    TenantId = SqliteWebApplicationFactory.TestTenantId,
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

        // Upload guards -- all three are implemented correctly by inspection today, but nothing
        // would catch a regression without a test.

        [Fact]
        public async Task An_upload_over_5MB_is_rejected_with_a_400()
        {
            var oversized = new byte[5 * 1024 * 1024 + 1];
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(oversized), "file", "import.xlsx");

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", content);

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
                .PostAsync("/api/Member/import?dryRun=true", content);

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
                .PostAsync("/api/Member/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Chưa chọn file", body.GetProperty("message").GetString());
        }

        // Handed over from this round's review: a file that is a well-formed ZIP but not a real
        // OOXML workbook -- the shape a truncated or corrupted download takes -- made ClosedXML's
        // XLWorkbook constructor throw NullReferenceException, which the controller's original
        // catch (InvalidOperationException or FormatException) did not cover, so it escaped as a
        // 500. Genuine non-zip garbage already worked, because ClosedXML throws a typed
        // FileFormatException (a FormatException) for that case.
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
                .PostAsync("/api/Member/import?dryRun=true", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = body.GetProperty("message").GetString();
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.Contains("Excel", message);
        }
    }
}
