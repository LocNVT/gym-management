using System;
using System.Collections.Generic;
using System.Linq;
using gym_management_server.Data.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Boots the real app for integration tests but replaces the SQL Server DbContext with an
    /// in-memory provider and injects the fingerprint encryption key, so no external DB is needed.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
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
    }
}
