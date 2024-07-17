using Volo.Abp.Modularity;

namespace GymManagement;

public abstract class GymManagementApplicationTestBase<TStartupModule> : GymManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
