using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin.Models;
using OrchardCore.BootswatchTheme.Drivers;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;

namespace OrchardCore.BootswatchTheme {
   public class Startup : StartupBase {
      public override void ConfigureServices(IServiceCollection services) {
         services.AddTransient<IConfigureOptions<ResourceManagementOptions>, ResourceManifestConfiguration>();
         services.AddDisplayDriver<Navbar, ToggleThemeNavbarDisplayDriver>();
      }
   }
}
