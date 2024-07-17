using System;
using System.Collections.Generic;
using System.Text;
using GymManagement.Localization;
using Volo.Abp.Application.Services;

namespace GymManagement;

/* Inherit your application services from this class.
 */
public abstract class GymManagementAppService : ApplicationService
{
    protected GymManagementAppService()
    {
        LocalizationResource = typeof(GymManagementResource);
    }
}
