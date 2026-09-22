using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.Trainers;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Invoices;
using gym_management_server.Repositories.Members;
using gym_management_server.Repositories.Trainers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ExportQueryTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task Member_export_skips_soft_deleted_rows()
        {
            using var db = NewContext();
            db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Còn", PhoneNumber = "0900000001" });
            db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Đã xóa", PhoneNumber = "0900000002", IsDeleted = true });
            await db.SaveChangesAsync();

            var rows = await new MemberRepository(db).GetForExportAsync();

            Assert.Single(rows);
            Assert.Equal("Còn", rows[0].FullName);
        }

        [Fact]
        public async Task Invoice_export_resolves_the_member_name_and_returns_its_line_items()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", PhoneNumber = "0900000003" };
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "HD001",
                MemberId = member.Id,
                InvoiceDate = new DateTime(2026, 5, 10),
                TotalAmount = 1_200_000m,
                Status = InvoiceStatus.Paid,
            };
            invoice.Items.Add(new InvoiceItem { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Description = "Gói 3 tháng", Quantity = 2, UnitPrice = 600_000m });
            db.Members.Add(member);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var (invoices, items) = await new InvoiceRepository(db).GetForExportAsync(null, null);

            Assert.Equal("Nguyễn Văn A", invoices[0].MemberName);
            Assert.Equal("HD001", items[0].InvoiceNumber);
            Assert.Equal(1_200_000m, items[0].LineTotal);   // Quantity * UnitPrice
        }

        [Fact]
        public async Task The_to_bound_includes_everything_that_happened_on_that_day()
        {
            using var db = NewContext();
            db.Invoices.Add(new Invoice { Id = Guid.NewGuid(), InvoiceNumber = "HD002", InvoiceDate = new DateTime(2026, 5, 31, 23, 30, 0) });
            await db.SaveChangesAsync();

            var (invoices, _) = await new InvoiceRepository(db)
                .GetForExportAsync(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

            Assert.Single(invoices);   // 23:30 on the last day must still be inside the range
        }

        [Fact]
        public async Task Export_query_caps_at_MaxRows_plus_one_so_the_row_limit_guard_still_fires()
        {
            // The `+ 1` matters: ExcelWriter.Write only throws when the row count *exceeds*
            // MaxRows, so a repository that truncated to exactly MaxRows would silently hand
            // the caller an incomplete file instead of the row-limit error.
            using var db = NewContext();
            var trainers = new List<Trainer>(ExcelWriter.MaxRows + 50);
            for (var i = 0; i < ExcelWriter.MaxRows + 50; i++)
            {
                trainers.Add(new Trainer { Id = Guid.NewGuid(), FullName = $"Trainer {i:D6}" });
            }
            db.Trainers.AddRange(trainers);
            await db.SaveChangesAsync();

            var rows = await new TrainerRepository(db).GetForExportAsync();

            Assert.Equal(ExcelWriter.MaxRows + 1, rows.Count);
        }
    }
}
