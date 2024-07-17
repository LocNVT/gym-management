using Volo.Abp.Settings;

namespace GymManagement.Settings;

public class GymManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(GymManagementSettings.MySetting1));
    }
}
