using System;
using System.Threading.Tasks;
using gym_management_server.Entities.Members;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Confirms the `RowVersion` concurrency tokens added per docs/ImprovementPlan.md (mục 2) are
    /// actually wired up at the schema level: a save based on a stale read must be rejected with
    /// <see cref="DbUpdateConcurrencyException"/> instead of silently overwriting a newer change.
    ///
    /// Uses a raw SQL update (rather than a second EF save) to change the stored RowVersion:
    /// SQLite has no native auto-incrementing "rowversion" column type the way SQL Server does, so
    /// a plain EF update here would leave the column unchanged and this test would pass for the
    /// wrong reason. Production runs on SQL Server (see appsettings.json), where the column really
    /// does get a new value on every write; this test instead verifies the half that is
    /// provider-independent - that EF rejects a save when its recorded original value no longer
    /// matches what's in the row.
    /// </summary>
    public class RowVersionConcurrencyTests
    {
        [Fact]
        public async Task SaveChanges_rejects_an_update_based_on_a_stale_RowVersion()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var member = new Member { Id = Guid.NewGuid(), FullName = "Original Name", PhoneNumber = "0988000222" };
            db.Members.Add(member);
            await db.SaveChangesAsync();
            db.Entry(member).State = EntityState.Detached;

            // The read a request makes before editing.
            var staleCopy = await db.Members.AsNoTracking().SingleAsync(m => m.Id == member.Id);
            Assert.Null(staleCopy.RowVersion);

            // Simulates another process changing the row - and, on SQL Server, the RowVersion
            // column auto-advancing - between this request's read and its write.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Members SET FullName = 'Edited By A', RowVersion = X'0102030405060708' WHERE Id = {member.Id}");

            // This save still carries the original (now stale) RowVersion as its concurrency
            // token and must be rejected, not silently overwrite the other change.
            db.Attach(staleCopy);
            staleCopy.FullName = "Edited By B";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        }
    }
}
