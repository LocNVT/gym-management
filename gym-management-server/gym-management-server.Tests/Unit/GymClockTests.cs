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
