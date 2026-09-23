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

        [Fact]
        public void LastMonths_of_1_returns_the_current_local_month()
        {
            var months = GymClock.LastMonths(1);
            var now = GymClock.LocalNow;

            Assert.Single(months);
            Assert.Equal((now.Year, now.Month), months[0]);
        }

        [Fact]
        public void LastMonths_crossing_year_boundary_produces_correct_sequence()
        {
            // Test that we get 13 consecutive months ending with current month,
            // even if it requires crossing January.
            var months = GymClock.LastMonths(13);
            var now = GymClock.LocalNow;

            Assert.Equal(13, months.Count);
            Assert.Equal((now.Year, now.Month), months[^1]);

            // Verify continuity: each month should be the previous month of the next
            for (int i = 1; i < months.Count; i++)
            {
                var (prevYear, prevMonth) = months[i - 1];
                var (curYear, curMonth) = months[i];
                var next = GymClock.PreviousMonth(curYear, curMonth) == (prevYear, prevMonth);
                Assert.True(next, $"Gap between months at index {i-1}");
            }
        }

        [Fact]
        public void LastMonths_of_0_returns_empty_list()
        {
            var months = GymClock.LastMonths(0);
            Assert.Empty(months);
        }

        [Fact]
        public void ToLocal_returns_Unspecified_kind()
        {
            var utc = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var local = GymClock.ToLocal(utc);

            Assert.Equal(DateTimeKind.Unspecified, local.Kind);
        }

        [Fact]
        public void ToUtc_returns_Utc_kind()
        {
            var local = new DateTime(2026, 9, 22, 17, 0, 0, DateTimeKind.Unspecified);
            var utc = GymClock.ToUtc(local);

            Assert.Equal(DateTimeKind.Utc, utc.Kind);
        }

        [Fact]
        public void LocalNow_is_not_Utc_kind()
        {
            var now = GymClock.LocalNow;

            Assert.NotEqual(DateTimeKind.Utc, now.Kind);
            Assert.Equal(DateTimeKind.Unspecified, now.Kind);
        }
    }
}
