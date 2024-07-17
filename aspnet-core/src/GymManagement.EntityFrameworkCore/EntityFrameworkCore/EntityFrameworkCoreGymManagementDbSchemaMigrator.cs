using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GymManagement.Data;
using Volo.Abp.DependencyInjection;

namespace GymManagement.EntityFrameworkCore;

public class EntityFrameworkCoreGymManagementDbSchemaMigrator
    : IGymManagementDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreGymManagementDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the GymManagementDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<GymManagementDbContext>()
            .Database
            .MigrateAsync();
    }
}
