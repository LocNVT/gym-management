using GymManagement.Samples;
using Xunit;

namespace GymManagement.EntityFrameworkCore.Applications;

[Collection(GymManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<GymManagementEntityFrameworkCoreTestModule>
{

}
