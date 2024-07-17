using Volo.Abp.Modularity;

namespace GymManagement;

/* Inherit from this class for your domain layer tests. */
public abstract class GymManagementDomainTestBase<TStartupModule> : GymManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
