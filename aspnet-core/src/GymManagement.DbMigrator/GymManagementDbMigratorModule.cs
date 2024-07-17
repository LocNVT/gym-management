using GymManagement.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace GymManagement.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(GymManagementEntityFrameworkCoreModule),
    typeof(GymManagementApplicationContractsModule)
    )]
public class GymManagementDbMigratorModule : AbpModule
{
}
