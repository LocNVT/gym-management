using GymManagement.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace GymManagement.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class GymManagementController : AbpControllerBase
{
    protected GymManagementController()
    {
        LocalizationResource = typeof(GymManagementResource);
    }
}
