using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Auth;
using gym_management_server.Infrastructure.Tenancy;
using gym_management_server.Repositories.OtpTokens;
using gym_management_server.Repositories.Tenants;
using gym_management_server.Repositories.Users;
using gym_management_server.Services.Auth;
using gym_management_server.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Public self-registration always creates a brand-new tenant with the registrant as its
    /// first Admin - see docs/ImprovementPlan.md mục 1. There is no "join an existing tenant"
    /// flow here; that's UserManagementService's job, for an already-authenticated Admin.
    /// </summary>
    public class AuthServiceTenantTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static AuthService BuildService(GymManagementContext db)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-signing-key-at-least-256-bits-long-for-hmacsha256",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                })
                .Build();

            return new AuthService(
                new UserRepository(db),
                new TenantRepository(db),
                new OtpTokenRepository(db),
                new EmailService(config),
                config);
        }

        private static RegisterInput SampleInput(string username) => new()
        {
            Username = username,
            Email = $"{username}@example.com",
            Password = "Password@123",
            FullName = "Chủ phòng gym",
            TenantName = $"Gym của {username}"
        };

        [Fact]
        public async Task Register_creates_a_new_tenant_and_makes_the_registrant_its_Admin()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var result = await service.RegisterAsync(SampleInput("owner1"));

            Assert.NotEqual(Guid.Empty, result.TenantId);
            Assert.Equal(1, result.Role); // Admin of their own new tenant

            var tenant = await db.Tenants.SingleAsync(t => t.Id == result.TenantId);
            Assert.Equal("Gym của owner1", tenant.Name);

            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Username == "owner1");
            Assert.Equal(result.TenantId, user.TenantId);
            Assert.Equal((byte)1, user.Role);
        }

        [Fact]
        public async Task Register_embeds_the_tenant_claim_in_the_issued_token()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var result = await service.RegisterAsync(SampleInput("owner2"));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
            var tenantClaim = jwt.Claims.Single(c => c.Type == HttpContextCurrentTenantAccessor.TenantIdClaimType);
            Assert.Equal(result.TenantId.ToString(), tenantClaim.Value);
        }

        [Fact]
        public async Task Two_self_registrations_never_share_a_tenant()
        {
            using var db = NewContext();
            var service = BuildService(db);

            var first = await service.RegisterAsync(SampleInput("owner3"));
            var second = await service.RegisterAsync(SampleInput("owner4"));

            Assert.NotEqual(first.TenantId, second.TenantId);
        }

        [Fact]
        public async Task Username_stays_globally_unique_across_different_prospective_tenants()
        {
            using var db = NewContext();
            var service = BuildService(db);

            await service.RegisterAsync(SampleInput("sametaken"));

            var ex = await Assert.ThrowsAsync<Exception>(() => service.RegisterAsync(SampleInput("sametaken")));
            Assert.Contains("đã tồn tại", ex.Message);
        }
    }
}
