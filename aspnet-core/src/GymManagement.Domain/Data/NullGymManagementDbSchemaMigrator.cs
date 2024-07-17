using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace GymManagement.Data;

/* This is used if database provider does't define
 * IGymManagementDbSchemaMigrator implementation.
 */
public class NullGymManagementDbSchemaMigrator : IGymManagementDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
