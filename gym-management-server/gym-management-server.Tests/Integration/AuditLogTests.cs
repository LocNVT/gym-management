using System;
using System.Linq;
using System.Threading.Tasks;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Expenses;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.Users;
using gym_management_server.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Covers the audit-log mechanism added per docs/ImprovementPlan.md (mục 6): Invoice,
    /// InvoiceItem, Expense, CheckIn and User are audited automatically by GymManagementContext's
    /// SaveChanges override, with no change needed in their services/controllers.
    /// </summary>
    public class AuditLogTests
    {
        private class FakeCurrentUser : ICurrentUserAccessor
        {
            public Guid? UserId { get; set; }
            public string? Username { get; set; }
        }

        [Fact]
        public async Task Creating_an_audited_entity_writes_one_Create_audit_row()
        {
            using var fixture = new SqliteDbFixture();
            using var db = fixture.NewContext();

            db.Expenses.Add(new Expense { Id = Guid.NewGuid(), ExpenseDate = DateTime.UtcNow, Category = "Rent", Amount = 5_000_000 });
            await db.SaveChangesAsync();

            var entry = await db.AuditLogs.SingleAsync();
            Assert.Equal(nameof(Expense), entry.EntityName);
            Assert.Equal(AuditAction.Create, entry.Action);
            Assert.Null(entry.OldValuesJson);
            Assert.Contains("5000000", entry.NewValuesJson);
        }

        [Fact]
        public async Task Updating_a_User_redacts_PasswordHash_from_the_audit_trail()
        {
            using var fixture = new SqliteDbFixture();
            using var db = fixture.NewContext();

            var user = new User { Id = Guid.NewGuid(), Username = "staff1", Email = "staff1@gym.com", FullName = "Staff One", PasswordHash = "super-secret-hash" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            user.FullName = "Staff One Renamed";
            await db.SaveChangesAsync();

            var updateEntry = await db.AuditLogs
                .Where(a => a.EntityName == nameof(User) && a.Action == AuditAction.Update)
                .SingleAsync();

            Assert.DoesNotContain("PasswordHash", updateEntry.OldValuesJson);
            Assert.DoesNotContain("PasswordHash", updateEntry.NewValuesJson);
            Assert.DoesNotContain("super-secret-hash", updateEntry.OldValuesJson + updateEntry.NewValuesJson);
            Assert.Contains("Staff One Renamed", updateEntry.NewValuesJson);
        }

        [Fact]
        public async Task Non_audited_entities_write_no_audit_rows()
        {
            using var fixture = new SqliteDbFixture();
            using var db = fixture.NewContext();

            var member = new Member { Id = Guid.NewGuid(), FullName = "Not Audited Yet", PhoneNumber = "0977000111" };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            member.FullName = "Still Not Audited";
            await db.SaveChangesAsync();

            Assert.Empty(await db.AuditLogs.ToListAsync());
        }

        [Fact]
        public async Task Deleting_an_audited_entity_writes_a_Delete_row_with_only_the_old_values()
        {
            using var fixture = new SqliteDbFixture();
            using var db = fixture.NewContext();

            var expense = new Expense { Id = Guid.NewGuid(), ExpenseDate = DateTime.UtcNow, Category = "Equipment", Amount = 1_200_000 };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            db.Expenses.Remove(expense);
            await db.SaveChangesAsync();

            var deleteEntry = await db.AuditLogs.Where(a => a.Action == AuditAction.Delete).SingleAsync();
            Assert.Null(deleteEntry.NewValuesJson);
            Assert.Contains("Equipment", deleteEntry.OldValuesJson);
        }

        [Fact]
        public async Task Audit_rows_capture_the_acting_user()
        {
            using var fixture = new SqliteDbFixture();
            var actorId = Guid.NewGuid();
            using var db = fixture.NewContext(new FakeCurrentUser { UserId = actorId, Username = "admin" });

            db.Expenses.Add(new Expense { Id = Guid.NewGuid(), ExpenseDate = DateTime.UtcNow, Category = "Utilities", Amount = 300_000 });
            await db.SaveChangesAsync();

            var entry = await db.AuditLogs.SingleAsync();
            Assert.Equal(actorId, entry.ActorUserId);
            Assert.Equal("admin", entry.ActorUsername);
        }
    }
}
