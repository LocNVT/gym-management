using GymManagement.Samples;
using Xunit;

namespace GymManagement.EntityFrameworkCore.Domains;

[Collection(GymManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<GymManagementEntityFrameworkCoreTestModule>
{

}
