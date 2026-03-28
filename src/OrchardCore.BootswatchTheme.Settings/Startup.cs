using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.BootswatchTheme.Settings.Drivers;
using OrchardCore.BootswatchTheme.Settings.Navigation;

namespace OrchardCore.BootswatchTheme.Settings {
   public class Startup : StartupBase {
      public override void ConfigureServices(IServiceCollection services) {
         services.AddScoped<IDisplayDriver<ISite>, BootswatchSettingsDisplayDriver>();
         services.AddScoped<INavigationProvider, BootswatchSettingsAdminMenu>();
         services.AddScoped<IPermissionProvider, Permissions>();
      }
   }
}
