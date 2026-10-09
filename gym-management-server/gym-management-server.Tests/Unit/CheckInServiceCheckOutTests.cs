using System;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Services;
using gym_management_server.Services.CheckIns;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// The manual check-out added per docs/ImprovementPlan.md mục 4, for staff to use when a
    /// member forgot to scan out (previously the only way to close a session was another scan).
    /// </summary>
    public class CheckInServiceCheckOutTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static CheckInService BuildService(GymManagementContext db) =>
            new(new GymManagementServiceMapObjects(), new CheckInRepository(db));

        [Fact]
        public async Task CheckOut_closes_an_open_session_and_records_the_operator()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Forgot To Scan", PhoneNumber = "0955555555" };
            var checkIn = new CheckIn { Id = Guid.NewGuid(), MemberId = member.Id, CheckInTime = DateTime.UtcNow.AddHours(-1) };
            db.Members.Add(member);
            db.CheckIns.Add(checkIn);
            await db.SaveChangesAsync();

            var staffId = Guid.NewGuid();
            var service = BuildService(db);

            var result = await service.CheckOutAsync(checkIn.Id, staffId);

            Assert.NotNull(result);
            Assert.NotNull(result!.CheckOutTime);
            Assert.Equal(CheckOutMethod.ManualByStaff, result.CheckOutMethod);

            var stored = await db.CheckIns.SingleAsync();
            Assert.NotNull(stored.CheckOutTime);
            Assert.Equal(CheckOutMethod.ManualByStaff, stored.CheckOutMethod);
            Assert.Equal(staffId, stored.OperatorUserId);
        }

        [Fact]
        public async Task CheckOut_rejects_a_session_already_checked_out()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Already Out", PhoneNumber = "0966666666" };
            var checkIn = new CheckIn
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                CheckInTime = DateTime.UtcNow.AddHours(-2),
                CheckOutTime = DateTime.UtcNow.AddHours(-1),
                CheckOutMethod = CheckOutMethod.Scan
            };
            db.Members.Add(member);
            db.CheckIns.Add(checkIn);
            await db.SaveChangesAsync();

            var service = BuildService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckOutAsync(checkIn.Id, Guid.NewGuid()));

            // Must not have overwritten the original check-out.
            var stored = await db.CheckIns.SingleAsync();
            Assert.Equal(CheckOutMethod.Scan, stored.CheckOutMethod);
        }

        [Fact]
        public async Task CheckOut_returns_null_for_an_unknown_session()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var result = await service.CheckOutAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.Null(result);
        }
    }
}
