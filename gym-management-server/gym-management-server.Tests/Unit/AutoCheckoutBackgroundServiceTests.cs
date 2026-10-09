using System;
using System.Threading;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;
using gym_management_server.Services.CheckIns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Covers the auto check-out job from docs/ImprovementPlan.md mục 4: nothing in this codebase
    /// ever closed a session nobody scanned out of, so a forgotten check-in stayed "active" forever.
    /// Uses a fixed TimeProvider instead of waiting for real hours to pass - see
    /// AutoCheckoutBackgroundService's class comment.
    /// </summary>
    public class AutoCheckoutBackgroundServiceTests
    {
        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FixedTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }

        private static GymManagementContext NewContext()
        {
            var options = new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GymManagementContext(options);
        }

        private static AutoCheckoutBackgroundService BuildService(DateTimeOffset now, int thresholdHours = 24)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Attendance:AutoCheckoutHours"] = thresholdHours.ToString(),
                })
                .Build();

            // Not used by CloseStaleSessionsAsync directly (it takes the context explicitly), only
            // required by the constructor; the full ExecuteAsync loop isn't under test here.
            var scopeFactory = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            return new AutoCheckoutBackgroundService(
                scopeFactory,
                new FixedTimeProvider(now),
                configuration,
                NullLogger<AutoCheckoutBackgroundService>.Instance);
        }

        [Fact]
        public async Task Closes_a_session_older_than_the_threshold()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Forgetful Member", PhoneNumber = "0911111111" };
            var checkInTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            db.Members.Add(member);
            db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), MemberId = member.Id, CheckInTime = checkInTime });
            await db.SaveChangesAsync();

            // 25 hours later - past the default 24h threshold.
            var service = BuildService(new DateTimeOffset(checkInTime).AddHours(25));

            var closed = await service.CloseStaleSessionsAsync(db, CancellationToken.None);

            Assert.Equal(1, closed);
            var session = await db.CheckIns.SingleAsync();
            Assert.NotNull(session.CheckOutTime);
            Assert.Equal(CheckOutMethod.AutoTimeout, session.CheckOutMethod);
            Assert.Equal(checkInTime.AddHours(24), session.CheckOutTime);
        }

        [Fact]
        public async Task Leaves_a_session_younger_than_the_threshold_open()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Still Inside", PhoneNumber = "0922222222" };
            var checkInTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            db.Members.Add(member);
            db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), MemberId = member.Id, CheckInTime = checkInTime });
            await db.SaveChangesAsync();

            // Only 2 hours later - well under the 24h threshold.
            var service = BuildService(new DateTimeOffset(checkInTime).AddHours(2));

            var closed = await service.CloseStaleSessionsAsync(db, CancellationToken.None);

            Assert.Equal(0, closed);
            var session = await db.CheckIns.SingleAsync();
            Assert.Null(session.CheckOutTime);
        }

        [Fact]
        public async Task Does_not_touch_a_session_already_checked_out()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Already Out", PhoneNumber = "0933333333" };
            var checkInTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            var checkOutTime = checkInTime.AddMinutes(45);
            db.Members.Add(member);
            db.CheckIns.Add(new CheckIn
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                CheckInTime = checkInTime,
                CheckOutTime = checkOutTime,
                CheckOutMethod = CheckOutMethod.Scan
            });
            await db.SaveChangesAsync();

            var service = BuildService(new DateTimeOffset(checkInTime).AddDays(10));

            var closed = await service.CloseStaleSessionsAsync(db, CancellationToken.None);

            Assert.Equal(0, closed);
            var session = await db.CheckIns.SingleAsync();
            Assert.Equal(checkOutTime, session.CheckOutTime);
            Assert.Equal(CheckOutMethod.Scan, session.CheckOutMethod);
        }

        [Fact]
        public async Task Honors_a_configured_threshold_other_than_24_hours()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Short Threshold Gym", PhoneNumber = "0944444444" };
            var checkInTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            db.Members.Add(member);
            db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), MemberId = member.Id, CheckInTime = checkInTime });
            await db.SaveChangesAsync();

            // 3 hours later, with a 2-hour threshold configured - should already be stale.
            var service = BuildService(new DateTimeOffset(checkInTime).AddHours(3), thresholdHours: 2);

            var closed = await service.CloseStaleSessionsAsync(db, CancellationToken.None);

            Assert.Equal(1, closed);
        }
    }
}
