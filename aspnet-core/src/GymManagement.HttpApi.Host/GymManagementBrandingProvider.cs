using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace GymManagement;

[Dependency(ReplaceServices = true)]
public class GymManagementBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "GymManagement";
}
