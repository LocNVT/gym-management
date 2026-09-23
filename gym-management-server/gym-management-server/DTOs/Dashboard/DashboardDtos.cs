namespace gym_management_server.DTOs.Dashboard
{
    /// <summary>
    /// Headline KPI figures for one calendar month (local time). Financial fields are nullable
    /// so the service layer can blank them out for roles that should not see money — the
    /// repository always populates every field; masking happens later, not here.
    /// </summary>
    /// <param name="UnpaidTotal">
    /// Outstanding debt as of now: the sum of every <c>Pending</c> invoice, regardless of when it
    /// was raised. This is a point-in-time balance ("how much is owed to us right now"), not a
    /// monthly flow like <paramref name="Revenue"/> or <paramref name="Expense"/> — it is
    /// deliberately NOT bounded by <paramref name="Year"/>/<paramref name="Month"/>, so it reads
    /// the same for every month queried. Do not "fix" that by scoping it to the month.
    /// </param>
    public record KpiOutput(
        int Year,
        int Month,
        decimal? Revenue,
        decimal? PreviousRevenue,
        decimal? Expense,
        decimal? PreviousExpense,
        decimal? Profit,
        decimal? UnpaidTotal,
        int ActiveMembers,
        int NewMembers,
        int PreviousNewMembers,
        int CurrentlyInside,
        int ExpiringIn30Days);

    /// <summary>One month's revenue/expense, for the trend chart.</summary>
    public record MonthPoint(int Year, int Month, decimal Revenue, decimal Expense);

    /// <summary>One month's new-member count, for the growth chart.</summary>
    public record GrowthPoint(int Year, int Month, int NewMembers);

    /// <summary>Breakdown of active subscriptions and revenue by service package.</summary>
    public record PackageSlice(string PackageName, int ActiveSubscriptions, decimal Revenue);

    /// <summary>Check-in counts bucketed by hour of day, for the busy-hours chart.</summary>
    public record HourSlice(int Hour, int CheckIns);

    /// <summary>A member whose subscription is expiring soon.</summary>
    public record ExpiringSoon(
        Guid MemberId,
        string MemberName,
        string MemberPhone,
        string PackageName,
        DateTime EndDate,
        int DaysLeft);
}
