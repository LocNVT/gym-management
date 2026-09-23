using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Infrastructure.Time;
using gym_management_server.Repositories.Reporting;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class DashboardChartQueryTests : IClassFixture<SqliteDbFixture>
    {
        private readonly SqliteDbFixture _fixture;
        public DashboardChartQueryTests(SqliteDbFixture fixture) => _fixture = fixture;

        private static Member AddMember(GymManagementContext db, string name, string phone)
        {
            var member = new Member { Id = Guid.NewGuid(), FullName = name, PhoneNumber = phone };
            db.Members.Add(member);
            return member;
        }

        [Fact]
        public async Task Revenue_trend_returns_one_point_per_month_oldest_first()
        {
            using var db = _fixture.NewContext();
            var points = await new DashboardRepository(db).GetRevenueTrendAsync(12);

            Assert.Equal(12, points.Count);
            var expected = GymClock.LastMonths(12);
            Assert.Equal(expected[0].Month, points[0].Month);
            Assert.Equal(expected[^1].Month, points[^1].Month);
        }

        [Fact]
        public async Task Months_with_no_activity_come_back_as_zero_not_missing()
        {
            using var db = _fixture.NewContext();
            var thisMonth = GymClock.LocalNow;
            db.Invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(), InvoiceNumber = "HD100", TotalAmount = 5_000_000m,
                Status = InvoiceStatus.Paid,
                InvoiceDate = GymClock.ToUtc(new DateTime(thisMonth.Year, thisMonth.Month, 15)),
            });
            await db.SaveChangesAsync();

            var points = await new DashboardRepository(db).GetRevenueTrendAsync(6);

            Assert.Equal(6, points.Count);
            Assert.All(points.Take(5), p => Assert.Equal(0m, p.Revenue));
            Assert.Equal(5_000_000m, points[^1].Revenue);
        }

        [Fact]
        public async Task Package_distribution_counts_active_subscriptions_and_their_revenue()
        {
            using var db = _fixture.NewContext();
            var member = AddMember(db, "Hội viên", "0900000201");
            var basic = new ServicePackage { Id = Guid.NewGuid(), Name = "Gói 1 tháng", Price = 500_000m, DurationDays = 30 };
            var premium = new ServicePackage { Id = Guid.NewGuid(), Name = "Gói 1 năm", Price = 5_000_000m, DurationDays = 365 };
            db.ServicePackages.AddRange(basic, premium);

            db.MemberDataServices.AddRange(
                new MemberDataService { Id = Guid.NewGuid(), MemberId = member.Id, ServicePackageId = basic.Id, PriceAtPurchase = 500_000m, StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(25), Status = SubscriptionStatus.Active },
                new MemberDataService { Id = Guid.NewGuid(), MemberId = member.Id, ServicePackageId = basic.Id, PriceAtPurchase = 450_000m, StartDate = DateTime.UtcNow.AddDays(-3), EndDate = DateTime.UtcNow.AddDays(27), Status = SubscriptionStatus.Active },
                new MemberDataService { Id = Guid.NewGuid(), MemberId = member.Id, ServicePackageId = premium.Id, PriceAtPurchase = 5_000_000m, StartDate = DateTime.UtcNow.AddDays(-400), EndDate = DateTime.UtcNow.AddDays(-35), Status = SubscriptionStatus.Expired });
            await db.SaveChangesAsync();

            var slices = await new DashboardRepository(db).GetPackageDistributionAsync();

            var basicSlice = slices.Single(s => s.PackageName == "Gói 1 tháng");
            Assert.Equal(2, basicSlice.ActiveSubscriptions);
            Assert.Equal(950_000m, basicSlice.Revenue);
            Assert.DoesNotContain(slices, s => s.PackageName == "Gói 1 năm");
        }

        [Fact]
        public async Task Peak_hours_always_returns_all_24_hours_bucketed_in_local_time()
        {
            using var db = _fixture.NewContext();
            var member = AddMember(db, "Hội viên", "0900000211");
            // 19:00 local is 12:00 UTC. Bucketing on the UTC hour would put this in 12.
            db.CheckIns.Add(new CheckIn
            {
                Id = Guid.NewGuid(), MemberId = member.Id,
                CheckInTime = GymClock.ToUtc(DateTime.Today.AddDays(-1).AddHours(19)),
            });
            await db.SaveChangesAsync();

            var hours = await new DashboardRepository(db).GetPeakHoursAsync(30);

            Assert.Equal(24, hours.Count);
            Assert.Equal(Enumerable.Range(0, 24), hours.Select(h => h.Hour));
            Assert.Equal(1, hours.Single(h => h.Hour == 19).CheckIns);
            Assert.Equal(0, hours.Single(h => h.Hour == 12).CheckIns);
        }

        [Fact]
        public async Task Expiring_soon_is_sorted_soonest_first_and_carries_the_phone_number()
        {
            using var db = _fixture.NewContext();
            var soon = AddMember(db, "Hết hạn sớm", "0900000221");
            var later = AddMember(db, "Hết hạn muộn", "0900000222");
            var package = new ServicePackage { Id = Guid.NewGuid(), Name = "Gói 3 tháng", Price = 1_500_000m, DurationDays = 90 };
            db.ServicePackages.Add(package);
            db.MemberDataServices.AddRange(
                new MemberDataService { Id = Guid.NewGuid(), MemberId = later.Id, ServicePackageId = package.Id, StartDate = DateTime.UtcNow.AddDays(-70), EndDate = DateTime.UtcNow.AddDays(20), Status = SubscriptionStatus.Active },
                new MemberDataService { Id = Guid.NewGuid(), MemberId = soon.Id, ServicePackageId = package.Id, StartDate = DateTime.UtcNow.AddDays(-87), EndDate = DateTime.UtcNow.AddDays(3), Status = SubscriptionStatus.Active });
            await db.SaveChangesAsync();

            var rows = await new DashboardRepository(db).GetExpiringSoonAsync(30);

            Assert.Equal(2, rows.Count);
            Assert.Equal("Hết hạn sớm", rows[0].MemberName);
            Assert.Equal("0900000221", rows[0].MemberPhone);
            Assert.Equal("Gói 3 tháng", rows[0].PackageName);
            Assert.InRange(rows[0].DaysLeft, 2, 3);
        }
    }
}
