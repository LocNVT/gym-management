# Business Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace "open the app, land on a list of members" with a dashboard that answers the questions a gym owner actually has: how much came in this month, how much went out, who is about to lapse, and when the place is busy.

**Architecture:** A reporting-only repository owns the aggregation queries and never touches the CRUD repositories, which stay small and single-purpose. Every aggregate is a `GroupBy` translated to SQL — nothing is pulled into memory and counted there. One endpoint per widget, so the KPI row paints immediately instead of waiting for the slowest query. A `GymClock` helper is the single place that knows the gym runs on UTC+7.

**Tech Stack:** ASP.NET Core 8, EF Core 9, xUnit, EF Core SQLite (tests only), Angular 20, chart.js + ng2-charts (already in `package.json`, currently unused).

**Spec:** `docs/superpowers/specs/2026-09-22-excel-dashboard-design.md` (section 7)

**Depends on:** `docs/superpowers/plans/2026-09-22-enum-and-excel-export.md` must be complete. This plan reads `InvoiceStatus.Paid`, `MemberStatus.Active` and `SubscriptionStatus.Active`, which do not exist before it, and it uses `CustomWebApplicationFactory.CreateAuthenticatedClient` added there. It does **not** depend on the import plan.

## Global Constraints

- Target framework is `net8.0`. Run tests with `dotnet test` from `gym-management-server/`.
- The gym's timezone is **UTC+7, fixed**. Vietnam has had no daylight saving since 1975, so a constant offset is correct. Every date boundary goes through `GymClock`; no `DateTime.Today`, no bare `.Date` on a UTC value.
- Aggregation runs in SQL. If a query materialises a table and counts in C#, it is wrong.
- **Revenue** is `Invoice.TotalAmount` where `Status == InvoiceStatus.Paid`. Never the sum of all invoices.
- Financial endpoints are Admin-only (`Roles = "1"`). `/kpi` is the one exception: it returns `null` financial fields to non-admins rather than `403`, so staff can still use the operational half of the page.
- Do not modify the existing `/attendance-dashboard` module. It is a live "who is in the building" view and serves a different purpose.

## A Correctness Trap, Read Before Task 2

`Invoice.TotalAmount` is supplied by the client; nothing sums it from `InvoiceItem`. This plan reports the stored value as-is, which is the honest thing to do — inventing a different number on the dashboard than the invoice screen shows would be worse. Do not "fix" it here; it is out of scope and belongs with the invoice module.

---

### Task 1: GymClock

**Files:**
- Create: `Infrastructure/Time/GymClock.cs`
- Test: `gym-management-server.Tests/Unit/GymClockTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces, in `gym_management_server.Infrastructure.Time`:
  - `static class GymClock` with
    - `const int OffsetHours = 7`
    - `static DateTime ToLocal(DateTime utc)` / `static DateTime ToUtc(DateTime local)`
    - `static DateTime LocalNow`
    - `static (DateTime From, DateTime To) MonthRangeUtc(int year, int month)` — `From` inclusive, `To` exclusive
    - `static (DateTime From, DateTime To) DayRangeUtc(DateTime localDate)`
    - `static (int Year, int Month) PreviousMonth(int year, int month)`
    - `static IReadOnlyList<(int Year, int Month)> LastMonths(int count)` — oldest first, ending with the current local month

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/GymClockTests.cs`:

```csharp
using gym_management_server.Infrastructure.Time;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class GymClockTests
    {
        [Fact]
        public void A_local_month_starts_at_1700_UTC_the_previous_day()
        {
            // 1 September 2026 00:00 in Ho Chi Minh City is 31 August 17:00 UTC.
            var (from, to) = GymClock.MonthRangeUtc(2026, 9);

            Assert.Equal(new DateTime(2026, 8, 31, 17, 0, 0), from);
            Assert.Equal(new DateTime(2026, 9, 30, 17, 0, 0), to);
        }

        [Fact]
        public void December_rolls_into_the_next_year()
        {
            var (from, to) = GymClock.MonthRangeUtc(2026, 12);

            Assert.Equal(new DateTime(2026, 11, 30, 17, 0, 0), from);
            Assert.Equal(new DateTime(2026, 12, 31, 17, 0, 0), to);
        }

        [Fact]
        public void A_local_day_is_a_24_hour_window_starting_at_1700_UTC()
        {
            var (from, to) = GymClock.DayRangeUtc(new DateTime(2026, 9, 22));

            Assert.Equal(new DateTime(2026, 9, 21, 17, 0, 0), from);
            Assert.Equal(new DateTime(2026, 9, 22, 17, 0, 0), to);
        }

        [Fact]
        public void An_evening_checkin_belongs_to_that_local_day_not_the_next_one()
        {
            // 22:00 local on 22 September is 15:00 UTC the same day. Using UTC .Date here
            // would be right by luck; the 01:00 local case below is the one that breaks.
            var (from, to) = GymClock.DayRangeUtc(new DateTime(2026, 9, 22));
            var lateEvening = GymClock.ToUtc(new DateTime(2026, 9, 22, 22, 0, 0));
            var justAfterMidnight = GymClock.ToUtc(new DateTime(2026, 9, 22, 1, 0, 0));

            Assert.InRange(lateEvening, from, to.AddTicks(-1));
            Assert.InRange(justAfterMidnight, from, to.AddTicks(-1));
        }

        [Fact]
        public void PreviousMonth_wraps_backwards_across_the_year()
        {
            Assert.Equal((2026, 8), GymClock.PreviousMonth(2026, 9));
            Assert.Equal((2025, 12), GymClock.PreviousMonth(2026, 1));
        }

        [Fact]
        public void LastMonths_is_oldest_first_and_ends_with_the_current_local_month()
        {
            var months = GymClock.LastMonths(12);
            var now = GymClock.LocalNow;

            Assert.Equal(12, months.Count);
            Assert.Equal((now.Year, now.Month), months[^1]);
            Assert.True(months[0].Year < months[^1].Year || months[0].Month < months[^1].Month);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~GymClockTests
```

Expected: compile error — `GymClock` does not exist.

- [ ] **Step 3: Write it**

Create `Infrastructure/Time/GymClock.cs`:

```csharp
namespace gym_management_server.Infrastructure.Time
{
    /// <summary>
    /// Everything stored is UTC; everything a gym owner reads is local. This is the only
    /// place that knows the difference.
    ///
    /// Vietnam is UTC+7 and has had no daylight saving since 1975, so a constant offset is
    /// correct and translates to a cheap DATEADD in SQL. If this system ever serves more
    /// than one timezone, this class is the first thing to replace.
    /// </summary>
    public static class GymClock
    {
        public const int OffsetHours = 7;

        public static DateTime ToLocal(DateTime utc) => utc.AddHours(OffsetHours);
        public static DateTime ToUtc(DateTime local) => local.AddHours(-OffsetHours);

        public static DateTime LocalNow => ToLocal(DateTime.UtcNow);

        /// <summary>UTC half-open range [From, To) covering one local calendar month.</summary>
        public static (DateTime From, DateTime To) MonthRangeUtc(int year, int month)
        {
            var localStart = new DateTime(year, month, 1);
            return (ToUtc(localStart), ToUtc(localStart.AddMonths(1)));
        }

        /// <summary>UTC half-open range [From, To) covering one local calendar day.</summary>
        public static (DateTime From, DateTime To) DayRangeUtc(DateTime localDate)
        {
            var localStart = localDate.Date;
            return (ToUtc(localStart), ToUtc(localStart.AddDays(1)));
        }

        public static (int Year, int Month) PreviousMonth(int year, int month) =>
            month == 1 ? (year - 1, 12) : (year, month - 1);

        /// <summary>The last <paramref name="count"/> local months, oldest first, ending with the current one.</summary>
        public static IReadOnlyList<(int Year, int Month)> LastMonths(int count)
        {
            var now = LocalNow;
            var anchor = new DateTime(now.Year, now.Month, 1);
            return Enumerable.Range(0, count)
                .Select(i => anchor.AddMonths(i - (count - 1)))
                .Select(d => (d.Year, d.Month))
                .ToList();
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~GymClockTests
```

Expected: 6 passing.

- [ ] **Step 5: Commit**

```bash
git add gym-management-server/
git commit -m "Add GymClock so date boundaries follow the gym's timezone, not UTC"
```

---

### Task 2: Dashboard DTOs and the KPI query

**Files:**
- Create: `DTOs/Dashboard/DashboardDtos.cs`
- Create: `Repositories/Reporting/IDashboardRepository.cs`, `Repositories/Reporting/DashboardRepository.cs`
- Create: `gym-management-server.Tests/Integration/SqliteDbFixture.cs`
- Test: `gym-management-server.Tests/Integration/DashboardKpiQueryTests.cs`

**Interfaces:**
- Consumes: `GymClock`, the enums.
- Produces, in `gym_management_server.DTOs.Dashboard`:
  - `record KpiOutput(int Year, int Month, decimal? Revenue, decimal? PreviousRevenue, decimal? Expense, decimal? PreviousExpense, decimal? Profit, decimal? UnpaidTotal, int ActiveMembers, int NewMembers, int PreviousNewMembers, int CurrentlyInside, int ExpiringIn30Days)`
  - `record MonthPoint(int Year, int Month, decimal Revenue, decimal Expense)`
  - `record GrowthPoint(int Year, int Month, int NewMembers)`
  - `record PackageSlice(string PackageName, int ActiveSubscriptions, decimal Revenue)`
  - `record HourSlice(int Hour, int CheckIns)`
  - `record ExpiringSoon(Guid MemberId, string MemberName, string MemberPhone, string PackageName, DateTime EndDate, int DaysLeft)`
- And `IDashboardRepository.Task<KpiOutput> GetKpiAsync(int year, int month)` returning every field populated (the service masks, not the repository).
- `SqliteDbFixture` with `GymManagementContext NewContext()` — a fresh schema-created SQLite in-memory database per call.

**Why SQLite rather than the InMemory provider:** the InMemory provider runs LINQ in memory, so a query that EF cannot translate to SQL still passes. These aggregates must translate; SQLite proves they do. A `GroupBy` that silently became client-side evaluation would work in tests and time out in production.

- [ ] **Step 1: Add the SQLite test package**

```bash
dotnet add gym-management-server/gym-management-server.Tests/gym-management-server.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.8
```

(No-op if the import plan already added it.)

- [ ] **Step 2: Write the fixture**

Create `gym-management-server.Tests/Integration/SqliteDbFixture.cs`:

```csharp
using gym_management_server.Data.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// A real SQL database for repository tests. The EF InMemory provider evaluates LINQ in
    /// memory, so a GroupBy that cannot be translated to SQL still passes there and then
    /// falls over on SQL Server. SQLite catches that.
    /// </summary>
    public sealed class SqliteDbFixture : IDisposable
    {
        private readonly List<SqliteConnection> _connections = new();

        public GymManagementContext NewContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            _connections.Add(connection);

            var context = new GymManagementContext(
                new DbContextOptionsBuilder<GymManagementContext>().UseSqlite(connection).Options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            foreach (var connection in _connections) connection.Dispose();
        }
    }
}
```

- [ ] **Step 3: Write the failing test**

Create `gym-management-server.Tests/Integration/DashboardKpiQueryTests.cs`:

```csharp
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

        private static Member Member(GymManagementContext db, string name, string phone,
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
            Member(db, "Đang tập", "0900000101");
            Member(db, "Tạm ngưng", "0900000102", MemberStatus.Suspended);
            var deleted = Member(db, "Đã xóa", "0900000103");
            deleted.IsDeleted = true;
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.ActiveMembers);
        }

        [Fact]
        public async Task New_members_counts_registrations_inside_the_local_month()
        {
            using var db = _fixture.NewContext();
            Member(db, "Tháng 9", "0900000111", registered: GymClock.ToUtc(new DateTime(2026, 9, 2)));
            Member(db, "Tháng 8", "0900000112", registered: GymClock.ToUtc(new DateTime(2026, 8, 28)));
            await db.SaveChangesAsync();

            var kpi = await new DashboardRepository(db).GetKpiAsync(2026, 9);

            Assert.Equal(1, kpi.NewMembers);
            Assert.Equal(1, kpi.PreviousNewMembers);
        }

        [Fact]
        public async Task Currently_inside_counts_sessions_with_no_checkout()
        {
            using var db = _fixture.NewContext();
            var a = Member(db, "Trong phòng", "0900000121");
            var b = Member(db, "Đã về", "0900000122");
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
            var member = Member(db, "Sắp hết hạn", "0900000131");
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
```

- [ ] **Step 4: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~DashboardKpiQueryTests
```

Expected: compile error — `DashboardRepository` does not exist.

- [ ] **Step 5: Write the DTOs**

Create `DTOs/Dashboard/DashboardDtos.cs` holding every record listed in **Interfaces** above. All financial fields on `KpiOutput` are nullable `decimal?` so the service can blank them for non-admins.

- [ ] **Step 6: Write the repository**

Create `Repositories/Reporting/IDashboardRepository.cs`:

```csharp
using gym_management_server.DTOs.Dashboard;

namespace gym_management_server.Repositories.Reporting
{
    /// <summary>
    /// Read-only aggregation for the dashboard. Deliberately separate from the CRUD
    /// repositories: they stay small and single-purpose, and nothing here writes.
    /// </summary>
    public interface IDashboardRepository
    {
        Task<KpiOutput> GetKpiAsync(int year, int month);
    }
}
```

Create `Repositories/Reporting/DashboardRepository.cs`:

```csharp
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
```

- [ ] **Step 7: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~DashboardKpiQueryTests
```

Expected: 7 passing.

- [ ] **Step 8: Commit**

```bash
git add gym-management-server/
git commit -m "Add the dashboard KPI query, tested against SQLite"
```

---

### Task 3: The chart queries

**Files:**
- Modify: `Repositories/Reporting/IDashboardRepository.cs`, `Repositories/Reporting/DashboardRepository.cs`
- Test: `gym-management-server.Tests/Integration/DashboardChartQueryTests.cs`

**Interfaces:**
- Consumes: Task 2's DTOs and `GymClock`.
- Produces, added to `IDashboardRepository`:
  - `Task<List<MonthPoint>> GetRevenueTrendAsync(int months)` — one point per month, oldest first, **including months with no data as zeroes** (a gap in a line chart reads as missing data, not as a bad month)
  - `Task<List<GrowthPoint>> GetMemberGrowthAsync(int months)` — same gap-filling
  - `Task<List<PackageSlice>> GetPackageDistributionAsync()`
  - `Task<List<HourSlice>> GetPeakHoursAsync(int days)` — always 24 entries, hour 0..23, in **local** time
  - `Task<List<ExpiringSoon>> GetExpiringSoonAsync(int days)` — soonest first

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Integration/DashboardChartQueryTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~DashboardChartQueryTests
```

Expected: compile error — the five methods are not on the repository.

- [ ] **Step 3: Implement the trend queries**

Add to `DashboardRepository`. The shape to copy for both trend methods — group in SQL, then left-join onto the full month list in memory so gaps become zeroes:

```csharp
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
```

`GetMemberGrowthAsync(int months)` follows the identical shape over `_db.Members`, filtering `!m.IsDeleted`, grouping `RegistrationDate` and counting rather than summing, producing `GrowthPoint`.

- [ ] **Step 4: Implement the remaining three queries**

```csharp
public async Task<List<PackageSlice>> GetPackageDistributionAsync() =>
    await _db.MemberDataServices
        .Where(s => s.Status == SubscriptionStatus.Active)
        .GroupBy(s => s.ServicePackage.Name)
        .Select(g => new PackageSlice(g.Key, g.Count(), g.Sum(x => x.PriceAtPurchase)))
        .OrderByDescending(s => s.ActiveSubscriptions)
        .ToListAsync();

public async Task<List<HourSlice>> GetPeakHoursAsync(int days)
{
    var from = DateTime.UtcNow.AddDays(-days);

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
    var horizon = now.AddDays(days);

    return await _db.MemberDataServices
        .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= now && s.EndDate <= horizon)
        .OrderBy(s => s.EndDate)
        .Select(s => new ExpiringSoon(
            s.MemberId,
            s.Member.FullName,
            s.Member.PhoneNumber,
            s.ServicePackage.Name,
            s.EndDate,
            EF.Functions.DateDiffDay(now, s.EndDate)))
        .ToListAsync();
}
```

**`EF.Functions.DateDiffDay` is SQL Server-only and will not run on SQLite.** Compute `DaysLeft` after materialising instead:

```csharp
    var rows = await _db.MemberDataServices
        .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= now && s.EndDate <= horizon)
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
```

Use the second version. The list is at most a few hundred rows, so projecting the day count client-side costs nothing and keeps the query portable.

- [ ] **Step 5: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~DashboardChartQueryTests
```

Expected: 5 passing. If `GroupBy(...AddHours(...))` throws a translation error on SQLite, it will also fail on SQL Server — do **not** work around it by calling `.ToListAsync()` before the `GroupBy`. Reshape the query so the grouping stays in SQL.

- [ ] **Step 6: Commit**

```bash
git add gym-management-server/
git commit -m "Add dashboard chart queries with gap-filled months and local-time hour buckets"
```

---

### Task 4: Service, controller and the role-based masking

**Files:**
- Create: `Services/Reporting/DashboardService.cs`, `Controllers/DashboardController.cs`
- Modify: `Program.cs`
- Test: `gym-management-server.Tests/Integration/DashboardApiTests.cs`

**Interfaces:**
- Consumes: `IDashboardRepository`.
- Produces:
  - `DashboardService.Task<KpiOutput> GetKpiAsync(int? year, int? month, bool includeFinancials)` — defaults to the current local month; blanks `Revenue`, `PreviousRevenue`, `Expense`, `PreviousExpense`, `Profit` and `UnpaidTotal` when `includeFinancials` is false.
  - Endpoints under `/api/Dashboard`: `GET /kpi?year=&month=` (`[Authorize]`), `GET /revenue-trend?months=12` (`[Authorize(Roles = "1")]`), `GET /member-growth?months=12` (`[Authorize]`), `GET /package-distribution` (`[Authorize(Roles = "1")]`), `GET /peak-hours?days=30` (`[Authorize]`), `GET /expiring-soon?days=30` (`[Authorize]`).

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Integration/DashboardApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class DashboardApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public DashboardApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Every_dashboard_endpoint_requires_authentication()
        {
            var client = _factory.CreateClient();
            foreach (var path in new[] { "kpi", "revenue-trend", "member-growth",
                                         "package-distribution", "peak-hours", "expiring-soon" })
            {
                var response = await client.GetAsync($"/api/Dashboard/{path}");
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task Staff_are_refused_the_financial_endpoints()
        {
            var staff = _factory.CreateAuthenticatedClient(role: 0);

            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Dashboard/revenue-trend")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Dashboard/package-distribution")).StatusCode);
        }

        [Fact]
        public async Task Staff_get_the_operational_KPIs_with_the_money_blanked_out()
        {
            // 403 on the whole endpoint would take the operational half of the page away too.
            var body = await _factory.CreateAuthenticatedClient(role: 0)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            Assert.Equal(JsonValueKind.Null, body.GetProperty("revenue").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("profit").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("unpaidTotal").ValueKind);

            Assert.Equal(JsonValueKind.Number, body.GetProperty("activeMembers").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("currentlyInside").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("expiringIn30Days").ValueKind);
        }

        [Fact]
        public async Task Admins_get_the_money()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 1)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            Assert.Equal(JsonValueKind.Number, body.GetProperty("revenue").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("profit").ValueKind);
        }

        [Fact]
        public async Task Kpi_defaults_to_the_current_local_month()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 1)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            var now = gym_management_server.Infrastructure.Time.GymClock.LocalNow;
            Assert.Equal(now.Year, body.GetProperty("year").GetInt32());
            Assert.Equal(now.Month, body.GetProperty("month").GetInt32());
        }

        [Fact]
        public async Task Peak_hours_returns_24_buckets_even_with_no_data()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 0)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/peak-hours");

            Assert.Equal(24, body.GetArrayLength());
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~DashboardApiTests
```

Expected: 404s.

- [ ] **Step 3: Write the service**

Create `Services/Reporting/DashboardService.cs`:

```csharp
using gym_management_server.DTOs.Dashboard;
using gym_management_server.Infrastructure.Time;
using gym_management_server.Repositories.Reporting;

namespace gym_management_server.Services.Reporting
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repository;
        public DashboardService(IDashboardRepository repository) => _repository = repository;

        public async Task<KpiOutput> GetKpiAsync(int? year, int? month, bool includeFinancials)
        {
            var now = GymClock.LocalNow;
            var kpi = await _repository.GetKpiAsync(year ?? now.Year, month ?? now.Month);

            // Staff keep the operational half of the page; only the money is withheld.
            return includeFinancials
                ? kpi
                : kpi with
                {
                    Revenue = null,
                    PreviousRevenue = null,
                    Expense = null,
                    PreviousExpense = null,
                    Profit = null,
                    UnpaidTotal = null,
                };
        }

        public Task<List<MonthPoint>> GetRevenueTrendAsync(int months) =>
            _repository.GetRevenueTrendAsync(Clamp(months, 1, 36));

        public Task<List<GrowthPoint>> GetMemberGrowthAsync(int months) =>
            _repository.GetMemberGrowthAsync(Clamp(months, 1, 36));

        public Task<List<PackageSlice>> GetPackageDistributionAsync() =>
            _repository.GetPackageDistributionAsync();

        public Task<List<HourSlice>> GetPeakHoursAsync(int days) =>
            _repository.GetPeakHoursAsync(Clamp(days, 1, 365));

        public Task<List<ExpiringSoon>> GetExpiringSoonAsync(int days) =>
            _repository.GetExpiringSoonAsync(Clamp(days, 1, 365));

        private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
    }
}
```

- [ ] **Step 4: Write the controller**

Create `Controllers/DashboardController.cs`:

```csharp
using gym_management_server.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service;
        public DashboardController(DashboardService service) => _service = service;

        private bool IsAdmin => User.IsInRole("1");

        /// <summary>
        /// One endpoint per widget rather than one big payload: the KPI row must paint
        /// immediately and should not wait for the check-in aggregation.
        /// </summary>
        [HttpGet("kpi")]
        public async Task<IActionResult> Kpi([FromQuery] int? year = null, [FromQuery] int? month = null)
            => Ok(await _service.GetKpiAsync(year, month, includeFinancials: IsAdmin));

        [HttpGet("revenue-trend")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> RevenueTrend([FromQuery] int months = 12)
            => Ok(await _service.GetRevenueTrendAsync(months));

        [HttpGet("member-growth")]
        public async Task<IActionResult> MemberGrowth([FromQuery] int months = 12)
            => Ok(await _service.GetMemberGrowthAsync(months));

        [HttpGet("package-distribution")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PackageDistribution()
            => Ok(await _service.GetPackageDistributionAsync());

        [HttpGet("peak-hours")]
        public async Task<IActionResult> PeakHours([FromQuery] int days = 30)
            => Ok(await _service.GetPeakHoursAsync(days));

        [HttpGet("expiring-soon")]
        public async Task<IActionResult> ExpiringSoon([FromQuery] int days = 30)
            => Ok(await _service.GetExpiringSoonAsync(days));
    }
}
```

- [ ] **Step 5: Register the dependencies**

In `Program.cs`:

```csharp
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<DashboardService>();
```

with `using gym_management_server.Repositories.Reporting;` and `using gym_management_server.Services.Reporting;`.

- [ ] **Step 6: Run the whole suite**

```bash
dotnet test gym-management-server/gym-management-server.sln
```

Expected: all green.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Add the dashboard API, with money hidden from staff rather than the whole page"
```

---

### Task 5: The dashboard page and its KPI row

**Files:**
- Create: `gym-management-client/src/app/shared/services/dashboard.service.ts`
- Create: `gym-management-client/src/app/modules/dashboard/dashboard.module.ts`, `dashboard-routing.module.ts`
- Create: `.../dashboard/components/dashboard/dashboard.component.ts`, `.html`, `.scss`
- Create: `.../dashboard/components/kpi-card/kpi-card.component.ts`, `.html`, `.scss`
- Modify: `gym-management-client/src/app/app-routing.module.ts`, `gym-management-client/src/app/shared/components/sidebar/sidebar.ts`
- Test: `gym-management-client/src/app/modules/dashboard/components/dashboard/dashboard.component.spec.ts`

**Interfaces:**
- Consumes: the endpoints from Task 4.
- Produces: `DashboardService` with `kpi(year?, month?)`, `revenueTrend(months)`, `memberGrowth(months)`, `packageDistribution()`, `peakHours(days)`, `expiringSoon(days)` — each returning an `Observable` of the matching interface. `<app-kpi-card [label] [value] [previous] [format]>` where `format` is `'currency' | 'number'`; a `null` value renders an em dash rather than `0`.

- [ ] **Step 1: Write the failing test**

Create `gym-management-client/src/app/modules/dashboard/components/dashboard/dashboard.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { KpiCardComponent } from '../kpi-card/kpi-card.component';
import { DashboardService, Kpi } from '../../../../shared/services/dashboard.service';

describe('DashboardComponent', () => {
    let fixture: ComponentFixture<DashboardComponent>;

    const kpi: Kpi = {
        year: 2026, month: 9,
        revenue: 12_000_000, previousRevenue: 10_000_000,
        expense: 4_000_000, previousExpense: 4_000_000,
        profit: 8_000_000, unpaidTotal: 1_000_000,
        activeMembers: 120, newMembers: 14, previousNewMembers: 10,
        currentlyInside: 7, expiringIn30Days: 9,
    };

    function configure(value: Kpi) {
        const stub: Partial<DashboardService> = {
            kpi: () => of(value),
            expiringSoon: () => of([]),
        };
        TestBed.configureTestingModule({
            declarations: [DashboardComponent, KpiCardComponent],
            providers: [{ provide: DashboardService, useValue: stub }],
        });
        fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
    }

    it('loads the KPI row on init', () => {
        configure(kpi);
        expect(fixture.componentInstance.kpi?.activeMembers).toBe(120);
    });

    it('reports the month-over-month change as a percentage', () => {
        configure(kpi);
        expect(fixture.componentInstance.changePercent(12_000_000, 10_000_000)).toBe(20);
    });

    it('treats growth from zero as no comparison rather than infinity', () => {
        configure(kpi);
        expect(fixture.componentInstance.changePercent(5_000_000, 0)).toBeNull();
    });

    it('hides the financial cards when the server blanked them', () => {
        configure({ ...kpi, revenue: null, profit: null, expense: null, unpaidTotal: null });
        expect(fixture.componentInstance.showFinancials).toBeFalse();
    });
});
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: cannot resolve the component.

- [ ] **Step 3: Write the API service**

Create `gym-management-client/src/app/shared/services/dashboard.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Kpi {
    year: number;
    month: number;
    revenue: number | null;
    previousRevenue: number | null;
    expense: number | null;
    previousExpense: number | null;
    profit: number | null;
    unpaidTotal: number | null;
    activeMembers: number;
    newMembers: number;
    previousNewMembers: number;
    currentlyInside: number;
    expiringIn30Days: number;
}

export interface MonthPoint { year: number; month: number; revenue: number; expense: number; }
export interface GrowthPoint { year: number; month: number; newMembers: number; }
export interface PackageSlice { packageName: string; activeSubscriptions: number; revenue: number; }
export interface HourSlice { hour: number; checkIns: number; }
export interface ExpiringSoon {
    memberId: string; memberName: string; memberPhone: string;
    packageName: string; endDate: string; daysLeft: number;
}

const BASE = '/api/Dashboard';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    constructor(private http: HttpClient) { }

    kpi(year?: number, month?: number): Observable<Kpi> {
        let params = new HttpParams();
        if (year) params = params.set('year', year);
        if (month) params = params.set('month', month);
        return this.http.get<Kpi>(`${BASE}/kpi`, { params });
    }

    revenueTrend(months = 12): Observable<MonthPoint[]> {
        return this.http.get<MonthPoint[]>(`${BASE}/revenue-trend`, {
            params: new HttpParams().set('months', months),
        });
    }

    memberGrowth(months = 12): Observable<GrowthPoint[]> {
        return this.http.get<GrowthPoint[]>(`${BASE}/member-growth`, {
            params: new HttpParams().set('months', months),
        });
    }

    packageDistribution(): Observable<PackageSlice[]> {
        return this.http.get<PackageSlice[]>(`${BASE}/package-distribution`);
    }

    peakHours(days = 30): Observable<HourSlice[]> {
        return this.http.get<HourSlice[]>(`${BASE}/peak-hours`, {
            params: new HttpParams().set('days', days),
        });
    }

    expiringSoon(days = 30): Observable<ExpiringSoon[]> {
        return this.http.get<ExpiringSoon[]>(`${BASE}/expiring-soon`, {
            params: new HttpParams().set('days', days),
        });
    }
}
```

- [ ] **Step 4: Write the KPI card**

`kpi-card.component.ts`:

```typescript
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-kpi-card',
    standalone: false,
    templateUrl: './kpi-card.component.html',
    styleUrls: ['./kpi-card.component.scss'],
})
export class KpiCardComponent {
    @Input({ required: true }) label!: string;
    @Input() value: number | null = null;
    @Input() previous: number | null = null;
    @Input() format: 'currency' | 'number' = 'number';
    @Input() icon = 'insights';

    /** null means "no meaningful comparison", not "no change". */
    get changePercent(): number | null {
        if (this.value === null || this.previous === null || this.previous === 0) return null;
        return Math.round(((this.value - this.previous) / this.previous) * 100);
    }

    get direction(): 'up' | 'down' | 'flat' {
        const change = this.changePercent;
        if (change === null || change === 0) return 'flat';
        return change > 0 ? 'up' : 'down';
    }
}
```

`kpi-card.component.html`:

```html
<div class="kpi-card">
  <mat-icon class="kpi-icon">{{ icon }}</mat-icon>
  <div class="kpi-body">
    <span class="kpi-label">{{ label }}</span>
    <span class="kpi-value">
      <ng-container *ngIf="value !== null; else blank">
        {{ format === 'currency' ? (value | number: '1.0-0') + ' ₫' : (value | number) }}
      </ng-container>
      <ng-template #blank>—</ng-template>
    </span>
    <span class="kpi-change" *ngIf="changePercent !== null" [class]="direction">
      <mat-icon>{{ direction === 'up' ? 'trending_up' : direction === 'down' ? 'trending_down' : 'trending_flat' }}</mat-icon>
      {{ changePercent > 0 ? '+' : '' }}{{ changePercent }}% so với tháng trước
    </span>
  </div>
</div>
```

`kpi-card.component.scss`:

```scss
.kpi-card {
  display: flex; gap: 14px; align-items: flex-start;
  padding: 18px; border-radius: 12px; background: #fff;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
}
.kpi-icon { color: #5c6bc0; }
.kpi-body { display: flex; flex-direction: column; gap: 4px; }
.kpi-label { font-size: 13px; color: rgba(0, 0, 0, 0.6); }
.kpi-value { font-size: 24px; font-weight: 600; }
.kpi-change {
  display: flex; align-items: center; gap: 4px; font-size: 12px;
  mat-icon { font-size: 16px; width: 16px; height: 16px; }
  &.up { color: #2e7d32; }
  &.down { color: #c62828; }
  &.flat { color: rgba(0, 0, 0, 0.5); }
}
```

- [ ] **Step 5: Write the page component**

`dashboard.component.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { DashboardService, ExpiringSoon, Kpi } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-dashboard',
    standalone: false,
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
    kpi: Kpi | null = null;
    expiring: ExpiringSoon[] = [];
    loadingKpi = true;

    constructor(private dashboard: DashboardService) { }

    /** Staff receive nulls here; there is nothing to show them. */
    get showFinancials(): boolean {
        return this.kpi?.revenue !== null && this.kpi?.revenue !== undefined;
    }

    changePercent(value: number | null, previous: number | null): number | null {
        if (value === null || previous === null || previous === 0) return null;
        return Math.round(((value - previous) / previous) * 100);
    }

    ngOnInit(): void {
        // Each widget loads on its own so the KPI row is not held up by the slower queries.
        this.dashboard.kpi().subscribe({
            next: (kpi) => { this.kpi = kpi; this.loadingKpi = false; },
            error: () => { this.loadingKpi = false; },
        });
        this.dashboard.expiringSoon(30).subscribe(rows => this.expiring = rows);
    }
}
```

`dashboard.component.html` — the KPI row only for now; charts arrive in Task 6:

```html
<h1 class="page-title">Tổng quan</h1>

<div class="kpi-grid" *ngIf="kpi as k">
  <app-kpi-card *ngIf="showFinancials" label="Doanh thu tháng này" icon="payments"
    [value]="k.revenue" [previous]="k.previousRevenue" format="currency"></app-kpi-card>
  <app-kpi-card *ngIf="showFinancials" label="Chi phí tháng này" icon="receipt_long"
    [value]="k.expense" [previous]="k.previousExpense" format="currency"></app-kpi-card>
  <app-kpi-card *ngIf="showFinancials" label="Lợi nhuận" icon="savings"
    [value]="k.profit" format="currency"></app-kpi-card>
  <app-kpi-card *ngIf="showFinancials" label="Công nợ chưa thu" icon="pending_actions"
    [value]="k.unpaidTotal" format="currency"></app-kpi-card>

  <app-kpi-card label="Hội viên đang hoạt động" icon="people" [value]="k.activeMembers"></app-kpi-card>
  <app-kpi-card label="Hội viên mới" icon="person_add"
    [value]="k.newMembers" [previous]="k.previousNewMembers"></app-kpi-card>
  <app-kpi-card label="Đang trong phòng" icon="directions_run" [value]="k.currentlyInside"></app-kpi-card>
  <app-kpi-card label="Sắp hết hạn (30 ngày)" icon="event_busy" [value]="k.expiringIn30Days"></app-kpi-card>
</div>

<app-loading *ngIf="loadingKpi"></app-loading>
```

`dashboard.component.scss`:

```scss
.page-title { font-size: 22px; font-weight: 600; margin: 0 0 18px; }
.kpi-grid {
  display: grid; gap: 16px;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
}
```

- [ ] **Step 6: Write the module and routing**

`dashboard-routing.module.ts`:

```typescript
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';

const routes: Routes = [{ path: '', component: DashboardComponent }];

@NgModule({ imports: [RouterModule.forChild(routes)], exports: [RouterModule] })
export class DashboardRoutingModule { }
```

`dashboard.module.ts` — declare `DashboardComponent` and `KpiCardComponent`, import `CommonModule`, `MatIconModule`, `MatCardModule`, `MatTableModule` and `DashboardRoutingModule`. Mirror the imports of an existing feature module such as `attendance-dashboard.module.ts`.

- [ ] **Step 7: Route to it and put it in the sidebar**

In `app-routing.module.ts`, add the lazy route and change the default redirect:

```typescript
{ path: '', redirectTo: '/dashboard', pathMatch: 'full' },
{
  path: 'dashboard',
  loadChildren: () => import('./modules/dashboard/dashboard.module').then(m => m.DashboardModule),
  canActivate: [AuthGuard]
},
```

Also change the wildcard route at the bottom from `redirectTo: '/members'` to `'/dashboard'`.

In `sidebar.ts`, add a group **before** `Quản lý`:

```typescript
{
  title: 'Tổng quan',
  items: [{ label: 'Bảng điều khiển', icon: 'space_dashboard', route: '/dashboard' }]
},
```

Leave the existing `Điểm danh → Tổng quan` item pointing at `/attendance-dashboard`; it is the live in-building view and is not replaced by this page.

- [ ] **Step 8: Run the tests and confirm they pass**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: 4 passing.

- [ ] **Step 9: Commit**

```bash
git add gym-management-client/
git commit -m "Add the dashboard page with a KPI row as the new landing page"
```

---

### Task 6: The charts and the expiring-soon table

**Files:**
- Create: `.../dashboard/components/revenue-chart/revenue-chart.component.ts`, `.html`
- Create: `.../dashboard/components/growth-chart/growth-chart.component.ts`, `.html`
- Create: `.../dashboard/components/peak-hours-chart/peak-hours-chart.component.ts`, `.html`
- Create: `.../dashboard/components/expiring-table/expiring-table.component.ts`, `.html`, `.scss`
- Modify: `dashboard.module.ts`, `dashboard.component.html`
- Test: `.../dashboard/components/revenue-chart/revenue-chart.component.spec.ts`

**Interfaces:**
- Consumes: `DashboardService` from Task 5.
- Produces: four self-loading widget components, each fetching its own endpoint in `ngOnInit` and showing its own loading and empty states. `RevenueChartComponent` exposes `labels: string[]` and `datasets: ChartDataset[]` for the assertions below.

**Each widget loads independently.** Do not gather the calls into the parent — the whole point of one endpoint per widget is that a slow query cannot block the rest of the page.

- [ ] **Step 1: Write the failing test**

Create `.../dashboard/components/revenue-chart/revenue-chart.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { RevenueChartComponent } from './revenue-chart.component';
import { DashboardService, MonthPoint } from '../../../../shared/services/dashboard.service';

describe('RevenueChartComponent', () => {
    let fixture: ComponentFixture<RevenueChartComponent>;

    function configure(stub: Partial<DashboardService>) {
        TestBed.configureTestingModule({
            declarations: [RevenueChartComponent],
            providers: [{ provide: DashboardService, useValue: stub }],
            schemas: [NO_ERRORS_SCHEMA],
        });
        fixture = TestBed.createComponent(RevenueChartComponent);
        fixture.detectChanges();
    }

    const points: MonthPoint[] = [
        { year: 2026, month: 8, revenue: 10_000_000, expense: 4_000_000 },
        { year: 2026, month: 9, revenue: 12_000_000, expense: 5_000_000 },
    ];

    it('labels the axis by month and year', () => {
        configure({ revenueTrend: () => of(points) });
        expect(fixture.componentInstance.labels).toEqual(['08/2026', '09/2026']);
    });

    it('plots revenue and expense as two series', () => {
        configure({ revenueTrend: () => of(points) });
        const datasets = fixture.componentInstance.datasets;

        expect(datasets.length).toBe(2);
        expect(datasets[0].data).toEqual([10_000_000, 12_000_000]);
        expect(datasets[1].data).toEqual([4_000_000, 5_000_000]);
    });

    it('shows a permission message instead of an error when staff open it', () => {
        configure({ revenueTrend: () => throwError(() => ({ status: 403 })) });
        expect(fixture.componentInstance.forbidden).toBeTrue();
        expect(fixture.componentInstance.loading).toBeFalse();
    });
});
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: cannot resolve the component.

- [ ] **Step 3: Write the revenue chart**

`revenue-chart.component.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { ChartConfiguration, ChartDataset } from 'chart.js';
import { DashboardService } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-revenue-chart',
    standalone: false,
    templateUrl: './revenue-chart.component.html',
})
export class RevenueChartComponent implements OnInit {
    labels: string[] = [];
    datasets: ChartDataset<'line'>[] = [];
    loading = true;
    /** Staff are refused this endpoint by design; say so rather than showing a failure. */
    forbidden = false;

    readonly options: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: {
            y: {
                beginAtZero: true,
                ticks: { callback: (v) => new Intl.NumberFormat('vi-VN').format(Number(v)) },
            },
        },
    };

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.revenueTrend(12).subscribe({
            next: (points) => {
                this.labels = points.map(p => `${String(p.month).padStart(2, '0')}/${p.year}`);
                this.datasets = [
                    { label: 'Doanh thu', data: points.map(p => p.revenue), borderColor: '#2e7d32', backgroundColor: 'rgba(46,125,50,0.12)', fill: true, tension: 0.3 },
                    { label: 'Chi phí', data: points.map(p => p.expense), borderColor: '#c62828', backgroundColor: 'rgba(198,40,40,0.12)', fill: true, tension: 0.3 },
                ];
                this.loading = false;
            },
            error: (err) => {
                this.forbidden = err.status === 403;
                this.loading = false;
            },
        });
    }
}
```

`revenue-chart.component.html`:

```html
<div class="chart-card">
  <h3>Doanh thu và chi phí 12 tháng</h3>
  <app-loading *ngIf="loading"></app-loading>
  <p *ngIf="forbidden" class="muted">Chỉ quản trị viên xem được số liệu tài chính.</p>
  <div class="chart-host" *ngIf="!loading && !forbidden">
    <canvas baseChart type="line" [labels]="labels" [datasets]="datasets" [options]="options"></canvas>
  </div>
</div>
```

- [ ] **Step 4: Write the other two charts**

`growth-chart.component.ts` — same structure, calls `memberGrowth(12)`, a single bar dataset `Hội viên mới` in `#5c6bc0`, `type="bar"`, title `Hội viên mới theo tháng`. No `forbidden` branch: staff may see it.

`peak-hours-chart.component.ts` — calls `peakHours(30)`, labels `['00:00', '01:00', … '23:00']` from `hour`, one bar dataset `Lượt check-in`, title `Giờ cao điểm (30 ngày qua)`. No `forbidden` branch.

- [ ] **Step 5: Write the expiring-soon table**

`expiring-table.component.ts`:

```typescript
import { Component, OnInit } from '@angular/core';
import { DashboardService, ExpiringSoon } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-expiring-table',
    standalone: false,
    templateUrl: './expiring-table.component.html',
    styleUrls: ['./expiring-table.component.scss'],
})
export class ExpiringTableComponent implements OnInit {
    rows: ExpiringSoon[] = [];
    loading = true;

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.expiringSoon(30).subscribe({
            next: (rows) => { this.rows = rows; this.loading = false; },
            error: () => { this.loading = false; },
        });
    }

    urgency(daysLeft: number): 'critical' | 'soon' | 'later' {
        if (daysLeft <= 3) return 'critical';
        if (daysLeft <= 7) return 'soon';
        return 'later';
    }
}
```

`expiring-table.component.html` — a plain table with columns Hội viên, Số điện thoại, Gói, Ngày hết hạn, Còn lại; the phone number is a `tel:` link so staff can call from the page, and the remaining-days cell carries `[class]="urgency(row.daysLeft)"`. Include an empty state: `Không có gói nào sắp hết hạn trong 30 ngày tới.`

`expiring-table.component.scss` — `.critical { color: #c62828; font-weight: 600; }`, `.soon { color: #ef6c00; }`, `.later { color: rgba(0,0,0,.6); }`.

- [ ] **Step 6: Register everything and place the widgets**

In `dashboard.module.ts`, declare the four new components and add `BaseChartDirective` to `imports`:

```typescript
import { BaseChartDirective } from 'ng2-charts';
// imports: [..., BaseChartDirective]
```

`ng2-charts` v8 exports `BaseChartDirective` as standalone, so it goes in `imports`, not `declarations`.

Append to `dashboard.component.html`, below the KPI grid:

```html
<div class="widget-grid">
  <app-revenue-chart class="wide"></app-revenue-chart>
  <app-growth-chart></app-growth-chart>
  <app-peak-hours-chart></app-peak-hours-chart>
  <app-expiring-table class="wide"></app-expiring-table>
</div>
```

And to `dashboard.component.scss`:

```scss
.widget-grid {
  display: grid; gap: 16px; margin-top: 20px;
  grid-template-columns: repeat(auto-fit, minmax(380px, 1fr));
  .wide { grid-column: 1 / -1; }
}
::ng-deep .chart-card {
  background: #fff; border-radius: 12px; padding: 18px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
  h3 { margin: 0 0 12px; font-size: 15px; font-weight: 600; }
  .chart-host { height: 300px; }
  .muted { color: rgba(0, 0, 0, 0.5); font-size: 13px; }
}
```

- [ ] **Step 7: Run the tests and build**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless && npm run build
```

Expected: 3 new tests passing, build clean.

- [ ] **Step 8: Verify by hand, as both roles**

Run both halves:

```bash
dotnet run --project gym-management-server/gym-management-server
```

```bash
cd gym-management-client && npm start
```

Sign in as `admin` / `Matkhau@2` (the seeded admin, role 1). Confirm `/dashboard` is where the app lands, the four financial cards show numbers, the revenue chart draws two series, and the expiring table lists anyone within 30 days.

Then create or use a staff account (role 0) and confirm: the financial cards are gone, the revenue chart shows the permission message instead of an error, and the rest of the page still works.

Finally, add a paid invoice dated late on the last day of the current month in local time and confirm it lands in this month's revenue, not next month's.

- [ ] **Step 9: Commit**

```bash
git add gym-management-client/
git commit -m "Add the dashboard charts and the expiring-subscriptions table"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| 7.1 reporting repository kept apart from the CRUD repositories | 2 |
| 7.2 six endpoints, one per widget | 4 |
| 7.3 revenue = paid invoices only; every metric defined | 2, proven in `DashboardKpiQueryTests` |
| 7.3 `TotalAmount` reported as stored, not recomputed | noted at the top; no task changes it |
| 7.4 fixed +7 offset, grouping stays in SQL | 1, 3 |
| 7.5 lazy module, chart.js/ng2-charts, `/dashboard` as the default route, `/attendance-dashboard` untouched | 5, 6 |
| 8 all endpoints authorised; `/kpi` masks instead of refusing | 4 |
| 9 unit + integration tests | every task |

**Gaps found and closed:**
- The spec says aggregation runs in SQL, but the existing test factory uses the EF InMemory provider, which evaluates LINQ in memory — a `GroupBy` that cannot be translated would pass in tests and fail in production. Task 2 adds a SQLite fixture and Task 3 step 5 says explicitly not to work around a translation failure by materialising early.
- The spec did not say what a chart should do for a month with no data. Left alone, chart.js draws a gap, which a reader interprets as missing data rather than a zero month. Task 3 fills the gaps explicitly and Task 3 step 1 tests it.
- The spec did not say what staff see where an admin sees a chart. Task 6 gives `RevenueChartComponent` a `forbidden` state so a 403 reads as a permission notice, not a crash.

**Type consistency:** `KpiOutput`'s thirteen members are spelled identically in the C# record (Task 2), the JSON assertions (Task 4) and the TypeScript `Kpi` interface (Task 5). `GetRevenueTrendAsync` / `revenueTrend`, `GetPeakHoursAsync` / `peakHours` and the rest pair up one-to-one between `IDashboardRepository`, `DashboardService`, `DashboardController` and `DashboardService` (TS). `MonthPoint`, `GrowthPoint`, `PackageSlice`, `HourSlice` and `ExpiringSoon` carry the same field names on both sides. `GymClock.OffsetHours` is referenced as a const in Tasks 1 and 3 so EF can inline it.
