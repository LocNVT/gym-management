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

        /// <summary>
        /// Converts UTC to local wall-clock time (Vietnam UTC+7).
        /// Returns <see cref="DateTimeKind.Unspecified"/> because the result has no correct kind in .NET:
        /// not <see cref="DateTimeKind.Utc"/> (it's not UTC), and not <see cref="DateTimeKind.Local"/>
        /// (which means the server's zone, a deployment detail). <see cref="DateTimeKind.Unspecified"/>
        /// also prevents System.Text.Json from appending a 'Z' and causing a client-side double conversion.
        /// </summary>
        public static DateTime ToLocal(DateTime utc) =>
            DateTime.SpecifyKind(utc.AddHours(OffsetHours), DateTimeKind.Unspecified);

        /// <summary>
        /// Converts local wall-clock time (Vietnam UTC+7) to UTC.
        /// Returns <see cref="DateTimeKind.Utc"/> because the result is a genuine UTC instant.
        /// </summary>
        public static DateTime ToUtc(DateTime local) =>
            DateTime.SpecifyKind(local.AddHours(-OffsetHours), DateTimeKind.Utc);

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
