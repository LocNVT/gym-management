using System;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Users;
using gym_management_server.Repositories.Users;
using gym_management_server.Services;
using gym_management_server.Services.Users;
using gym_management_server.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// An Admin adding staff accounts to their own tenant - see docs/ImprovementPlan.md mục 1.
    /// Uses SQLite (not InMemory) because this exercises the global query filter for real.
    /// </summary>
    public class UserManagementServiceTests
    {
        private static UserManagementService BuildService(GymManagementContext db) =>
            new(new UserRepository(db), new GymManagementServiceMapObjects());

        [Fact]
        public async Task CreateAsync_stamps_the_new_account_with_the_callers_tenant()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();
            var tenant = Guid.NewGuid();
            db.CurrentTenant.SetTenant(tenant);

            var service = BuildService(db);
            var created = await service.CreateAsync(new CreateUserInput
            {
                Username = "staff1",
                Email = "staff1@gym.com",
                Password = "Password@123",
                FullName = "Nhân viên Một",
                Role = 0
            });

            var stored = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == created.Id);
            Assert.Equal(tenant, stored.TenantId);
        }

        [Fact]
        public async Task GetListAsync_only_shows_the_callers_own_tenant()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            db.CurrentTenant.SetTenant(tenantA);
            await BuildService(db).CreateAsync(new CreateUserInput
            { Username = "a-staff", Email = "a@gym.com", Password = "Password@123", FullName = "A Staff" });

            db.CurrentTenant.SetTenant(tenantB);
            await BuildService(db).CreateAsync(new CreateUserInput
            { Username = "b-staff", Email = "b@gym.com", Password = "Password@123", FullName = "B Staff" });

            db.CurrentTenant.SetTenant(tenantA);
            var listForA = await BuildService(db).GetListAsync();

            Assert.Single(listForA.Items);
            Assert.Equal("a-staff", listForA.Items[0].Username);
        }

        [Fact]
        public async Task SetActiveAsync_cannot_reach_a_user_in_another_tenant()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            db.CurrentTenant.SetTenant(tenantA);
            var createdInA = await BuildService(db).CreateAsync(new CreateUserInput
            { Username = "only-in-a", Email = "onlyina@gym.com", Password = "Password@123", FullName = "Only In A" });

            db.CurrentTenant.SetTenant(tenantB);
            var result = await BuildService(db).SetActiveAsync(createdInA.Id, isActive: false);

            Assert.Null(result);
        }
    }
}
