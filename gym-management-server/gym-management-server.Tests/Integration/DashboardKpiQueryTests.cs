using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Expenses;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Infrastructure.Time;
using gym_management_server.Repositories.Reporting;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class DashboardKpiQueryTests : IClassFixture<SqliteDbFixture>
    {
        private readonly SqliteDbFixture _fixture;
        public DashboardKpiQueryTests(SqliteDbFixture fixture) => _fixture = fixture;

        // Named AddMember rather than Member: a method named identically to the entity type
        // it constructs and returns is a resolution trap (Member vs. new Member { ... }).
        private static Member AddMember(GymManagementContext db, string name, string phone,
            MemberStatus status = MemberStatus.Active, DateTime? registered = null)
        {
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FullName = name,
                PhoneNumber = phone,
                Status = status,
                RegistrationDate = registered ?? GymClock.ToUtc(new DateTime(2026, 9, 5)),
            };
            db.Members.Add(member);
            return member;
        }

        private static void Invoice(GymManagementContext db, decimal amount, InvoiceStatus status, DateTime localDate)
            => db.Invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = Guid.NewGuid().ToString("N")[..8],
                TotalAmount = amount,
                Status = status,
                InvoiceDate = GymClock.ToUtc(localDate),
            });

        [Fact]
        public async Task Revenue_counts_paid_invoices_only()
        {
            using var db = _fixture.NewContext();
            Invoice(db, 1_000_000m, InvoiceStatus.Paid, new DateTime(2026, 9, 10));
            Invoice(db, 500_000m, InvoiceStatus.Paid, new DateTime(2026, 9, 20));
            Invoice(db, 9_000_000m, InvoiceStatus.Pending, new DateTime(2026, 9, 15));
            Invoice(db, 7_000_000m, InvoiceStatus.Cancelled, new DateTime(2026, 9, 16));
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1_500_000m, kpi.Revenue);
            Assert.Equal(9_000_000m, kpi.UnpaidTotal);
        }

        [Fact]
        public async Task An_invoice_late_on_the_last_local_day_of_the_month_belongs_to_that_month()
        {
            using var db = _fixture.NewContext();
            // 30 Sep 23:30 local is 30 Sep 16:30 UTC — still September locally.
            Invoice(db, 400_000m, InvoiceStatus.Paid, new DateTime(2026, 9, 30, 23, 30, 0));
            // 1 Oct 00:30 local is 30 Sep 17:30 UTC — October, despite the UTC date.
            Invoice(db, 800_000m, InvoiceStatus.Paid, new DateTime(2026, 10, 1, 0, 30, 0));
            await db.SaveChangesAsync();

            var september = await new DashboardRepository(db).GetKpiAsync(2026, 9);
            var october = await new DashboardRepository(db).GetKpiAsync(2026, 10);

            Assert.Equal(400_000m, september.Revenue);
            Assert.Equal(800_000m, october.Revenue);
        }

        [Fact]
        public async Task Profit_is_revenue_minus_expense_and_the_previous_month_is_carried_for_comparison()
        {
            using var db = _fixture.NewContext();
            Invoice(db, 3_000_000m, InvoiceStatus.Paid, new DateTime(2026, 9, 10));
            Invoice(db, 2_000_000m, InvoiceStatus.Paid, new DateTime(2026, 8, 10));
            db.Expenses.Add(new Expense
            {
                Id = Guid.NewGuid(), Category = "Tiền thuê", Amount = 1_200_000m,
                ExpenseDate = GymClock.ToUtc(new DateTime(2026, 9, 3)),
            });
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(3_000_000m, kpi.Revenue);
            Assert.Equal(1_200_000m, kpi.Expense);
            Assert.Equal(1_800_000m, kpi.Profit);
            Assert.Equal(2_000_000m, kpi.PreviousRevenue);
        }

        [Fact]
        public async Task Active_members_excludes_suspended_and_soft_deleted_rows()
        {
            using var db = _fixture.NewContext();
            AddMember(db, "Đang tập", "0900000101");
            AddMember(db, "Tạm ngưng", "0900000102", MemberStatus.Suspended);
            var deleted = AddMember(db, "Đã xóa", "0900000103");
            deleted.IsDeleted = true;
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.ActiveMembers);
        }

        [Fact]
        public async Task New_members_counts_registrations_inside_the_local_month()
        {
            using var db = _fixture.NewContext();
            AddMember(db, "Tháng 9", "0900000111", registered: GymClock.ToUtc(new DateTime(2026, 9, 2)));
            AddMember(db, "Tháng 8", "0900000112", registered: GymClock.ToUtc(new DateTime(2026, 8, 28)));
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.NewMembers);
            Assert.Equal(1, kpi.PreviousNewMembers);
        }

        [Fact]
        public async Task Currently_inside_counts_sessions_with_no_checkout()
        {
            using var db = _fixture.NewContext();
            var a = AddMember(db, "Trong phòng", "0900000121");
            var b = AddMember(db, "Đã về", "0900000122");
            db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), MemberId = a.Id, CheckInTime = DateTime.UtcNow.AddHours(-1) });
            db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), MemberId = b.Id, CheckInTime = DateTime.UtcNow.AddHours(-3), CheckOutTime = DateTime.UtcNow.AddHours(-2) });
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.CurrentlyInside);
        }

        [Fact]
        public async Task Expiring_in_30_days_counts_active_subscriptions_only()
        {
            using var db = _fixture.NewContext();
            var member = AddMember(db, "Sắp hết hạn", "0900000131");
            var package = new ServicePackage { Id = Guid.NewGuid(), Name = "Gói 3 tháng", Price = 1_000_000m, DurationDays = 90 };
            db.ServicePackages.Add(package);
            db.MemberDataServices.Add(new MemberDataService
            {
                Id = Guid.NewGuid(), MemberId = member.Id, ServicePackageId = package.Id,
                StartDate = DateTime.UtcNow.AddDays(-80), EndDate = DateTime.UtcNow.AddDays(10),
                Status = SubscriptionStatus.Active,
            });
            db.MemberDataServices.Add(new MemberDataService
            {
                Id = Guid.NewGuid(), MemberId = member.Id, ServicePackageId = package.Id,
                StartDate = DateTime.UtcNow.AddDays(-200), EndDate = DateTime.UtcNow.AddDays(5),
                Status = SubscriptionStatus.Cancelled,   // cancelled: not "expiring"
            });
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.ExpiringIn30Days);
        }
    }
}
