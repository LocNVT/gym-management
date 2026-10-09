using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Boots the real app for integration tests but replaces the SQL Server DbContext with an
    /// in-memory provider and injects the fingerprint encryption key, so no external DB is needed.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Every authenticated client this factory issues carries this tenant - see
        /// GymManagementContext's global query filter (docs/ImprovementPlan.md mục 1). A test that
        /// seeds rows directly through a DI scope (bypassing the HTTP pipeline, so there's no JWT
        /// for GymManagementContext to read a tenant from) must set this same TenantId explicitly,
        /// or those rows are invisible to every authenticated request.
        /// </summary>
        public static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Fingerprint:EncryptionKey"] = Convert.ToBase64String(new byte[32]) // deterministic test key
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove every EF Core / SqlServer registration so only the in-memory provider remains.
                var toRemove = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<GymManagementContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(GymManagementContext) ||
                        (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false) ||
                        (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<GymManagementContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        }

        /// <summary>An HttpClient carrying a valid JWT. role: 0 = Staff, 1 = Admin.</summary>
        public HttpClient CreateAuthenticatedClient(byte role)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", IssueToken(role));
            return client;
        }

        private string IssueToken(byte role)
        {
            // Must match appsettings.json's Jwt section, which the test host loads as-is.
            var config = Services.GetRequiredService<IConfiguration>().GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Key"]!));

            var token = new JwtSecurityToken(
                issuer: config["Issuer"],
                audience: config["Audience"],
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, role == 1 ? "admin" : "staff"),
                    new Claim(ClaimTypes.Role, role.ToString()),
                    new Claim(HttpContextCurrentTenantAccessor.TenantIdClaimType, TestTenantId.ToString()),
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
