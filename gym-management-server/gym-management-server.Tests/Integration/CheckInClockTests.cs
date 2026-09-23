using gym_management_server.Entities.CheckIns;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Services;
using gym_management_server.Services.CheckIns;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Manual check-ins used to be stamped with <c>DateTime.Now</c> while fingerprint
    /// check-ins used <c>DateTime.UtcNow</c>, so the CheckIns table carried two clocks seven
    /// hours apart and anything that grouped by day or hour was wrong for half its rows.
    ///
    /// These tests assert on <see cref="DateTimeKind"/> rather than comparing the value to
    /// <c>DateTime.UtcNow</c>: on a machine whose local zone IS UTC the two calls return the
    /// same instant, so a value comparison would pass against the very bug it is meant to
    /// catch. The Kind differs on every machine.
    /// </summary>
    public class CheckInClockTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static async Task<Member> SeedMemberAsync(GymManagementContext db)
        {
            var member = new Member
            {
                Id = Guid.NewGuid(),
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0901000001",
            };
            db.Members.Add(member);
            await db.SaveChangesAsync();
            return member;
        }

        [Fact]
        public async Task A_manual_check_in_is_stamped_in_UTC()
        {
            using var db = NewContext();
            var member = await SeedMemberAsync(db);
            var service = new CheckInService(new GymManagementServiceMapObjects(), new CheckInRepository(db));

            var created = await service.CreateAsync(new CheckInCreateInput
            {
                MemberId = member.Id,
                Method = CheckInMethod.Card,
            });

            Assert.Equal(DateTimeKind.Utc, created.CheckInTime.Kind);
        }

        [Fact]
        public async Task A_manual_check_in_and_a_fingerprint_check_in_share_one_clock()
        {
            // The two write paths must agree. This is the property that was broken.
            using var db = NewContext();
            var member = await SeedMemberAsync(db);
            var service = new CheckInService(new GymManagementServiceMapObjects(), new CheckInRepository(db));

            var manual = await service.CreateAsync(new CheckInCreateInput
            {
                MemberId = member.Id,
                Method = CheckInMethod.Card,
            });

            // What the fingerprint path stamps, verbatim from FingerprintService.VerifyAsync.
            var fingerprintClock = DateTime.UtcNow;

            Assert.Equal(fingerprintClock.Kind, manual.CheckInTime.Kind);
            Assert.True(
                Math.Abs((fingerprintClock - manual.CheckInTime).TotalMinutes) < 1,
                $"The two check-in paths disagree by {(fingerprintClock - manual.CheckInTime).TotalHours:F1} hours. " +
                "Both must stamp UTC.");
        }
    }
}
