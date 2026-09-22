using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ExportApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public ExportApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        private void Seed(Action<GymManagementContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
            seed(db);
            db.SaveChanges();
        }

        [Fact]
        public async Task Export_requires_authentication()
        {
            var response = await _factory.CreateClient().GetAsync("/api/Member/export");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Staff_may_not_export_financial_data()
        {
            var staff = _factory.CreateAuthenticatedClient(role: 0);

            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Invoice/export")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Expense/export")).StatusCode);
        }

        [Fact]
        public async Task Export_returns_an_xlsx_with_a_header_row_and_one_row_per_member()
        {
            Seed(db =>
            {
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", PhoneNumber = "0911000001" });
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Trần Thị B", PhoneNumber = "0911000002" });
            });

            var response = await _factory.CreateAuthenticatedClient(role: 0).GetAsync("/api/Member/export");
            response.EnsureSuccessStatusCode();

            Assert.Equal(ExcelWriter.ContentType, response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("thanh-vien", response.Content.Headers.ContentDisposition!.FileNameStar
                ?? response.Content.Headers.ContentDisposition!.FileName!);

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            var ws = wb.Worksheet("Thành viên");
            Assert.Equal("Họ và tên", ws.Cell(1, 1).GetString());
            Assert.Equal(3, ws.LastRowUsed()!.RowNumber());   // header + 2 members
        }

        [Fact]
        public async Task Invoice_export_carries_its_line_items_on_a_second_sheet()
        {
            var admin = _factory.CreateAuthenticatedClient(role: 1);
            var response = await admin.GetAsync("/api/Invoice/export");
            response.EnsureSuccessStatusCode();

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.True(wb.Worksheets.Contains("Hóa đơn"));
            Assert.True(wb.Worksheets.Contains("Chi tiết hóa đơn"));
        }

        // Ruling H: this test seeds 50,001 members into its own factory instance, never the shared
        // IClassFixture one — CustomWebApplicationFactory keys its in-memory database per instance,
        // and sharing it here would silently corrupt the exact-row-count assertion above (xUnit does
        // not guarantee test order within a class, so the corruption would be intermittent).
        [Fact]
        public async Task Export_over_the_row_limit_is_rejected_with_400()
        {
            using var factory = new CustomWebApplicationFactory();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                for (var i = 0; i <= ExcelWriter.MaxRows; i++)
                    db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = $"HV{i}", PhoneNumber = $"09{i:D8}" });
                db.SaveChanges();
            }

            var response = await factory.CreateAuthenticatedClient(role: 0).GetAsync("/api/Member/export");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("vượt giới hạn", await response.Content.ReadAsStringAsync());
        }
    }
}
