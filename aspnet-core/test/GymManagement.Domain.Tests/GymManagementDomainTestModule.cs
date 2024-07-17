using Volo.Abp.Modularity;

namespace GymManagement;

[DependsOn(
    typeof(GymManagementDomainModule),
    typeof(GymManagementTestBaseModule)
)]
public class GymManagementDomainTestModule : AbpModule
{

}
