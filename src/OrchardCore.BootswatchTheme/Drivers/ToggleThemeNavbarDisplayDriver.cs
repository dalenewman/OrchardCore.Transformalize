using OrchardCore;
using OrchardCore.Admin.Models;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Themes.Services;

namespace OrchardCore.BootswatchTheme.Drivers {
   public sealed class ToggleThemeNavbarDisplayDriver : DisplayDriver<Navbar> {
      private readonly ISiteThemeService _siteThemeService;

      public ToggleThemeNavbarDisplayDriver(ISiteThemeService siteThemeService) {
         _siteThemeService = siteThemeService;
      }

      public override IDisplayResult Display(Navbar model, BuildDisplayContext context) {
         // TheTheme's own driver only renders for TheTheme itself, not derived
         // themes. Reuse its toggle view and Orchard's persisted appearance choice.
         return View("ToggleTheme", model)
            .RenderWhen(async () => await _siteThemeService.GetSiteThemeNameAsync() == "BootswatchTheme")
            .Location(OrchardCoreConstants.DisplayType.Detail, "Content:10");
      }
   }
}
