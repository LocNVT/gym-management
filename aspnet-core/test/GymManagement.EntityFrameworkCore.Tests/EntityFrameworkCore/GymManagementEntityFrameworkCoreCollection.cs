using Xunit;

namespace GymManagement.EntityFrameworkCore;

[CollectionDefinition(GymManagementTestConsts.CollectionDefinitionName)]
public class GymManagementEntityFrameworkCoreCollection : ICollectionFixture<GymManagementEntityFrameworkCoreFixture>
{

}
