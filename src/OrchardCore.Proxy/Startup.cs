using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using ProxyModule;
using ProxyModule.Drivers;
using ProxyModule.Handlers;
using ProxyModule.Models;

namespace OrchardCore.Proxy {
   public class Startup : StartupBase {
      public override void ConfigureServices(IServiceCollection services) {

         // orchard cms services
         services.AddScoped<IDataMigration, Migrations>();
         // services.AddScoped<IPermissionProvider, Permissions>();
         services.AddScoped<IContentHandler, ProxyHandler>();

         // parts
         services.AddContentPart<ProxyPart>().UseDisplayDriver<ProxyPartDisplayDriver>();

      }

      public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) {

         routes.MapAreaControllerRoute(
             name: "Home",
             areaName: Common.ModuleName,
             pattern: "Proxy/{ContentItemId}/{*path}",
             defaults: new { controller = "Proxy", action = "Index" }
         );

      }
   }
}
