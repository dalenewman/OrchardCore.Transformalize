using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.Themes.Services;

namespace OrchardCore.BootswatchTheme.Settings.Navigation {
   public class BootswatchSettingsAdminMenu : AdminNavigationProvider {
      private const string ThemeId = "BootswatchTheme";

      private readonly IStringLocalizer T;
      private readonly ISiteThemeService _siteThemeService;

      public BootswatchSettingsAdminMenu(
            IStringLocalizer<BootswatchSettingsAdminMenu> localizer,
            ISiteThemeService siteThemeService) {
         T = localizer;
         _siteThemeService = siteThemeService;
      }

      protected override async ValueTask BuildAsync(NavigationBuilder builder) {

         var siteTheme = await _siteThemeService.GetSiteThemeAsync();
         if (siteTheme?.Id != ThemeId) {
            return;
         }

         builder.Add(T["Design"], design => design
            .Add(T["Bootswatch"], bootswatch => bootswatch
               .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = Constants.SettingsGroupId })
               .Permission(Permissions.ManageBootswatchSettings)
               .LocalNav()
            )
         );
      }
   }
}
