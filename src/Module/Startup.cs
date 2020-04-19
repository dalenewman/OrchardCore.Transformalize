using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Module.Fields;
using Module.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;
using Fluid;
using OrchardCore.Data.Migration;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display;
using Module.Drivers;
using Module.ViewModels;

namespace Module {
   public class Startup : StartupBase {

      public Startup() {
         TemplateContext.GlobalMemberAccessStrategy.Register<TransformalizeArrangementField>();
         TemplateContext.GlobalMemberAccessStrategy.Register<DisplayTransformalizeArrangementFieldViewModel>();
      }

      public override void ConfigureServices(IServiceCollection services) {

         services.AddSession();

         services.AddScoped<IDataMigration, Migrations>();
         services.AddScoped<IResourceManifestProvider, ResourceManifest>();

         services.AddContentField<TransformalizeArrangementField>(); // UseDisplayDriver in dev branch
         services.AddScoped<IContentFieldDisplayDriver, TransformalizeArrangementFieldDisplayDriver>();
         services.AddContentPart<TransformalizeArrangementPart>();
         

      }

      public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) {

         routes.MapAreaControllerRoute(
             name: "Transformalize.Report.Index",
             areaName: "OrchardCore.Transformalize",
             pattern: "report/{ContentItemId}",
             defaults: new { controller = "Report", action = "Index" }
         );

         builder.UseSession();
      }
   }
}