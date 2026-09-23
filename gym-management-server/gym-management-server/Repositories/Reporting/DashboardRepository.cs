using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Dashboard;
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Reporting
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly GymManagementContext _db;
        public DashboardRepository(GymManagementContext db) => _db = db;

        public async Task<KpiOutput> GetKpiAsync(int year, int month)
        {
            var (from, to) = GymClock.MonthRangeUtc(year, month);
            var (prevYear, prevMonth) = GymClock.PreviousMonth(year, month);
            var (prevFrom, prevTo) = GymClock.MonthRangeUtc(prevYear, prevMonth);

            var now = DateTime.UtcNow;
            var (todayStartUtc, _) = GymClock.DayRangeUtc(GymClock.LocalNow);
            var expiryHorizon = now.AddDays(30);

            // Each of these is a scalar aggregate pushed down to SQL.
            var revenue = await PaidTotalAsync(from, to);
            var previousRevenue = await PaidTotalAsync(prevFrom, prevTo);

            var expense = await _db.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var previousExpense = await _db.Expenses
                .Where(e => e.ExpenseDate >= prevFrom && e.ExpenseDate < prevTo)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            // Point-in-time balance, not a monthly flow: every Pending invoice regardless of
            // date, so this is deliberately not bounded by `from`/`to`. See the XML doc on
            // KpiOutput.UnpaidTotal — do not "fix" this by adding a date filter.
            var unpaid = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Pending)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

            var activeMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.Status == MemberStatus.Active);

            // !IsDeleted here too, though the brief's prose didn't spell it out: a soft-deleted
            // registrant isn't a net-new member any more than a soft-deleted member is "active".
            // Keeps NewMembers/PreviousNewMembers consistent with ActiveMembers above.
            var newMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.RegistrationDate >= from && m.RegistrationDate < to);

            var previousNewMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.RegistrationDate >= prevFrom && m.RegistrationDate < prevTo);

            var currentlyInside = await _db.CheckIns.CountAsync(c => c.CheckOutTime == null);

            // Lower bound is the start of today in local terms, not the current instant: a
            // subscription expiring today must still count as expiring, not have already
            // rolled off the moment local wall-clock time passes the UTC-stored EndDate
            // (which for a local-midnight EndDate happens hours before local midnight).
            var expiring = await _db.MemberDataServices
                .CountAsync(s => !s.Member.IsDeleted && s.Status == SubscriptionStatus.Active
                              && s.EndDate >= todayStartUtc && s.EndDate <= expiryHorizon);

            return new KpiOutput(
                Year: year,
                Month: month,
                Revenue: revenue,
                PreviousRevenue: previousRevenue,
                Expense: expense,
                PreviousExpense: previousExpense,
                Profit: revenue - expense,
                UnpaidTotal: unpaid,
                ActiveMembers: activeMembers,
                NewMembers: newMembers,
                PreviousNewMembers: previousNewMembers,
                CurrentlyInside: currentlyInside,
                ExpiringIn30Days: expiring);
        }

        /// <summary>Revenue is paid invoices only — never the sum of everything issued.</summary>
        private async Task<decimal> PaidTotalAsync(DateTime from, DateTime to) =>
            await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.InvoiceDate >= from && i.InvoiceDate < to)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        public async Task<List<MonthPoint>> GetRevenueTrendAsync(int months)
        {
            var wanted = GymClock.LastMonths(months);
            var (from, _) = GymClock.MonthRangeUtc(wanted[0].Year, wanted[0].Month);
            var (_, to) = GymClock.MonthRangeUtc(wanted[^1].Year, wanted[^1].Month);

            // AddHours translates to DATEADD, so the grouping happens in SQL on the local month.
            var revenue = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.InvoiceDate >= from && i.InvoiceDate < to)
                .GroupBy(i => new
                {
                    i.InvoiceDate.AddHours(GymClock.OffsetHours).Year,
                    i.InvoiceDate.AddHours(GymClock.OffsetHours).Month,
                })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var expenses = await _db.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .GroupBy(e => new
                {
                    e.ExpenseDate.AddHours(GymClock.OffsetHours).Year,
                    e.ExpenseDate.AddHours(GymClock.OffsetHours).Month,
                })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            // A month with no rows must appear as zero: a gap in a line chart reads as missing data.
            return wanted.Select(m => new MonthPoint(
                m.Year,
                m.Month,
                revenue.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Total ?? 0m,
                expenses.FirstOrDefault(e => e.Year == m.Year && e.Month == m.Month)?.Total ?? 0m)).ToList();
        }

        public async Task<List<GrowthPoint>> GetMemberGrowthAsync(int months)
        {
            var wanted = GymClock.LastMonths(months);
            var (from, _) = GymClock.MonthRangeUtc(wanted[0].Year, wanted[0].Month);
            var (_, to) = GymClock.MonthRangeUtc(wanted[^1].Year, wanted[^1].Month);

            // AddHours translates to DATEADD, so the grouping happens in SQL on the local month.
            var growth = await _db.Members
                .Where(m => !m.IsDeleted && m.RegistrationDate >= from && m.RegistrationDate < to)
                .GroupBy(m => new
                {
                    m.RegistrationDate.AddHours(GymClock.OffsetHours).Year,
                    m.RegistrationDate.AddHours(GymClock.OffsetHours).Month,
                })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // A month with no registrations must appear as zero: a gap in a line chart reads as missing data.
            return wanted.Select(m => new GrowthPoint(
                m.Year,
                m.Month,
                growth.FirstOrDefault(g => g.Year == m.Year && g.Month == m.Month)?.Count ?? 0)).ToList();
        }

        public async Task<List<PackageSlice>> GetPackageDistributionAsync()
        {
            // The grouping and aggregates run in SQL; constructing the PackageSlice record
            // itself inside the Select does not translate on SQLite, so materialise into an
            // anonymous type first and build the record client-side from that.
            var rows = await _db.MemberDataServices
                .Where(s => !s.Member.IsDeleted && s.Status == SubscriptionStatus.Active)
                .GroupBy(s => s.ServicePackage.Name)
                .Select(g => new { PackageName = g.Key, ActiveSubscriptions = g.Count(), Revenue = g.Sum(x => x.PriceAtPurchase) })
                .OrderByDescending(s => s.ActiveSubscriptions)
                .ToListAsync();

            return rows.Select(r => new PackageSlice(r.PackageName, r.ActiveSubscriptions, r.Revenue)).ToList();
        }

        public async Task<List<HourSlice>> GetPeakHoursAsync(int days)
        {
            var from = DateTime.UtcNow.AddDays(-days);

            // AddHours translates to DATEADD, so the grouping happens in SQL on the local hour.
            var counted = await _db.CheckIns
                .Where(c => c.CheckInTime >= from)
                .GroupBy(c => c.CheckInTime.AddHours(GymClock.OffsetHours).Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            // Always 24 buckets: a bar chart missing its quiet hours is misleading.
            return Enumerable.Range(0, 24)
                .Select(h => new HourSlice(h, counted.FirstOrDefault(c => c.Hour == h)?.Count ?? 0))
                .ToList();
        }

        public async Task<List<ExpiringSoon>> GetExpiringSoonAsync(int days)
        {
            var now = DateTime.UtcNow;
            var (todayStartUtc, _) = GymClock.DayRangeUtc(GymClock.LocalNow);
            var horizon = now.AddDays(days);

            // EF.Functions.DateDiffDay is SQL Server-only and will not translate on SQLite, so
            // DaysLeft is computed after materialising the (at most a few hundred) rows rather
            // than projected in SQL.
            //
            // Lower bound is the start of today in local terms (see GetKpiAsync's ExpiringIn30Days
            // for why): a subscription whose EndDate is today's local midnight, stored as
            // yesterday-17:00 UTC, must still appear in the call list instead of disappearing
            // the moment local wall-clock time passes that instant.
            var rows = await _db.MemberDataServices
                .Where(s => !s.Member.IsDeleted && s.Status == SubscriptionStatus.Active
                         && s.EndDate >= todayStartUtc && s.EndDate <= horizon)
                .OrderBy(s => s.EndDate)
                .Select(s => new
                {
                    s.MemberId,
                    s.Member.FullName,
                    s.Member.PhoneNumber,
                    PackageName = s.ServicePackage.Name,
                    s.EndDate,
                })
                .ToListAsync();

            return rows.Select(r => new ExpiringSoon(
                r.MemberId, r.FullName, r.PhoneNumber, r.PackageName, r.EndDate,
                (int)Math.Ceiling((r.EndDate - now).TotalDays))).ToList();
        }
    }
}
