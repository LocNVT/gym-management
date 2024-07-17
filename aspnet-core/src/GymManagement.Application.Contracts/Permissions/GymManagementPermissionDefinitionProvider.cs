using GymManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace GymManagement.Permissions;

public class GymManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(GymManagementPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(GymManagementPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GymManagementResource>(name);
    }
}
