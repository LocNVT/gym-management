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

            var unpaid = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Pending)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

            var activeMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.Status == MemberStatus.Active);

            var newMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.RegistrationDate >= from && m.RegistrationDate < to);

            var previousNewMembers = await _db.Members
                .CountAsync(m => !m.IsDeleted && m.RegistrationDate >= prevFrom && m.RegistrationDate < prevTo);

            var currentlyInside = await _db.CheckIns.CountAsync(c => c.CheckOutTime == null);

            var expiring = await _db.MemberDataServices
                .CountAsync(s => s.Status == SubscriptionStatus.Active
                              && s.EndDate >= now && s.EndDate <= expiryHorizon);

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
    }
}
