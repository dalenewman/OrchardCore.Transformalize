using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.BootswatchTheme.Settings.Navigation {
   public class BootswatchSettingsAdminMenu : AdminNavigationProvider {
      private readonly IStringLocalizer T;

      public BootswatchSettingsAdminMenu(IStringLocalizer<BootswatchSettingsAdminMenu> localizer) {
         T = localizer;
      }

      protected override ValueTask BuildAsync(NavigationBuilder builder) {

         builder.Add(T["Design"], design => design
            .Add(T["Bootswatch"], bootswatch => bootswatch
               .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = Constants.SettingsGroupId })
               .Permission(Permissions.ManageBootswatchSettings)
               .LocalNav()
            )
         );

         return ValueTask.CompletedTask;
      }
   }
}
