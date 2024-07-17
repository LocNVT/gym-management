using Volo.Abp.Modularity;

namespace GymManagement;

[DependsOn(
    typeof(GymManagementApplicationModule),
    typeof(GymManagementDomainTestModule)
)]
public class GymManagementApplicationTestModule : AbpModule
{

}
