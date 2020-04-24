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
using Module.Drivers;
using Module.ViewModels;
using Module.Services.Contracts;
using Module.Services;
using Module.Handlers;
using OrchardCore.ContentManagement.Handlers;
using Microsoft.AspNetCore.Http;

namespace Module {
   public class Startup : StartupBase {

      public Startup() {
         TemplateContext.GlobalMemberAccessStrategy.Register<TransformalizeArrangementField>();
         TemplateContext.GlobalMemberAccessStrategy.Register<DisplayTransformalizeArrangementFieldViewModel>();
      }

      public override void ConfigureServices(IServiceCollection services) {

         services.AddSession();
         services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

         services.AddScoped<ILinkService, LinkService>();
         services.AddScoped<ISortService, SortService>();
         services.AddScoped<IStickyParameterService, StickyParameterService>();
         services.AddScoped<IReportLoadService, ReportLoadService>();
         services.AddScoped<IReportRunService, ReportRunService>();

         services.AddScoped<IDataMigration, Migrations>();
         services.AddScoped<IResourceManifestProvider, ResourceManifest>();

         services.AddContentField<TransformalizeArrangementField>(); // UseDisplayDriver in dev branch
         services.AddScoped<IContentFieldDisplayDriver, TransformalizeArrangementFieldDisplayDriver>();
         services.AddContentPart<TransformalizeArrangementPart>();

         services.AddScoped<IContentHandler, TransformalizeHandler>();
         services.AddScoped<IContentPermissionsService, ContentPermissionsService>();
      }

      public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) {

         routes.MapAreaControllerRoute(
             name: "Transformalize.Report.Index",
             areaName: Common.ModuleName,
             pattern: "report/{ContentItemId}",
             defaults: new { controller = "Report", action = "Index" }
         );

         routes.MapAreaControllerRoute(
             name: "Transformalize.Export.Index",
             areaName: Common.ModuleName,
             pattern: "export/{ContentItemId}",
             defaults: new { controller = "Export", action = "Index" }
         );

         builder.UseSession();
      }
   }
}