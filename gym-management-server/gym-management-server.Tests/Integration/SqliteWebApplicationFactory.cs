using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Boots the app against SQLite held open in memory.
    ///
    /// The EF Core InMemory provider used by CustomWebApplicationFactory is wrong for import
    /// tests: it ignores transactions (so a rollback test would pass without rolling anything
    /// back) and it does not enforce unique indexes (so a duplicate-phone test would pass with
    /// the duplicate check deleted). SQLite honours both. Do not swap this back.
    /// </summary>
    public class SqliteWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <summary>See CustomWebApplicationFactory.TestTenantId - same purpose, same value.</summary>
        public static readonly Guid TestTenantId = CustomWebApplicationFactory.TestTenantId;

        private DbConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Fingerprint:EncryptionKey"] = Convert.ToBase64String(new byte[32]),
                }));

            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<GymManagementContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(GymManagementContext) ||
                        (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false) ||
                        (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                // Kept open for the lifetime of the factory; closing it drops the database.
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<GymManagementContext>(options => options.UseSqlite(_connection));
            });

            builder.ConfigureServices(services =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                scope.ServiceProvider.GetRequiredService<GymManagementContext>()
                     .Database.EnsureCreated();
            });
        }

        public HttpClient CreateAuthenticatedClient(byte role)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", IssueToken(role));
            return client;
        }

        private string IssueToken(byte role)
        {
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection?.Dispose();
        }
    }
}
