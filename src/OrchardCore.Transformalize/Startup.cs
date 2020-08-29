using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TransformalizeModule.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;
using OrchardCore.Data.Migration;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using TransformalizeModule.Drivers;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.Services;
using TransformalizeModule.Handlers;
using OrchardCore.ContentManagement.Handlers;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Settings;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using TransformalizeModule.Navigation;
using Transformalize.Contracts;
using Transformalize.Logging;
using OrchardCore.Workflows.Helpers;
using TransformalizeModule.Activities;

namespace TransformalizeModule {
   public class Startup : StartupBase {

      public override void ConfigureServices(IServiceCollection services) {

         // services.AddSession();
         services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

         // transformalize services
         services.AddScoped(sp => new MemoryLogger(LogLevel.Info));
         services.AddScoped(typeof(CombinedLogger<>));
         services.AddScoped<ILinkService, LinkService>();
         services.AddScoped<ISortService, SortService>();
         services.AddScoped<IArrangementService, ArrangementService>();
         services.AddScoped<IArrangementLoadService, ArrangementLoadService>();
         services.AddScoped<IArrangementRunService, ArrangementRunService>();
         services.AddScoped<IArrangementSchemaService, ArrangementSchemaService>();
         services.AddScoped<IParameterService, ParameterService>();
         services.AddScoped<ICommonService, CommonService>();
         services.AddScoped<IReportService, ReportService>();
         services.AddScoped<ITaskService, TaskService>();
         services.AddScoped<IFormService, FormService>();
         services.AddScoped<ISchemaService, SchemaService>();
         services.AddScoped<ISettingsService, SettingsService>();
         services.AddScoped<ITransformalizeParametersModifier, TransformalizeParametersModifier>();

         services.AddTransient<IConfigurationContainer, OrchardConfigurationContainer>();
         services.AddTransient<IContainer, OrchardContainer>();

         // orchard cms services
         services.AddScoped<IDataMigration, Migrations>();
         services.AddScoped<IPermissionProvider, Permissions>();
         services.AddScoped<IResourceManifestProvider, ResourceManifest>();
         services.AddScoped<IContentHandler, TransformalizeHandler>();

         // parts
         services.AddContentPart<TransformalizeReportPart>().UseDisplayDriver<TransformalizeReportPartDisplayDriver>();
         services.AddContentPart<TransformalizeTaskPart>().UseDisplayDriver<TransformalizeTaskPartDisplayDriver>();

         // settings
         services.AddScoped<IDisplayDriver<ISite>, TransformalizeSettingsDisplayDriver>();
         services.AddScoped<INavigationProvider, TransformalizeSettingsAdminMenu>();

         // activities
         services.AddActivity<TransformalizeActivity, TransformalizeActivityDisplayDriver>();
      }

      public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) {

         RouteReporting(routes);
         RouteMap(routes);
         RouteCalendar(routes);
         RouteTasks(routes);
         RouteBulkActions(routes);

         routes.MapAreaControllerRoute(
             name: "Transformalize Parameters",
             areaName: Common.ModuleName,
             pattern: "t/tp/{ContentItemId}",
             defaults: new { controller = "Arrangement", action = "TransformalizeParameters" }
         );

         routes.MapAreaControllerRoute(
             name: "Schema API",
             areaName: Common.ModuleName,
             pattern: "t/schema/{format}/{ContentItemId}",
             defaults: new { controller = "Schema", action = "Index", format = "xml" }
         );

         // builder.UseSession();
      }

      public void RouteBulkActions(IEndpointRouteBuilder routes) {

         routes.MapAreaControllerRoute(
             name: null,
             areaName: Common.ModuleName,
             pattern: "t/action/create",
             defaults: new { controller = "BulkAction", action = "Create" }
         );

         routes.MapAreaControllerRoute(
             name: null,
             areaName: Common.ModuleName,
             pattern: "t/action/review",
             defaults: new { controller = "BulkAction", action = "Review" }
         );

         routes.MapAreaControllerRoute(
             name: null,
             areaName: Common.ModuleName,
             pattern: "t/action/review/form",
             defaults: new { controller = "BulkAction", action = "Form" }
         );

         routes.MapAreaControllerRoute(
             name: null,
             areaName: Common.ModuleName,
             pattern: "t/action/run",
             defaults: new { controller = "BulkAction", action = "Run" }
         );

         routes.MapAreaControllerRoute(
             name: null,
             areaName: Common.ModuleName,
             pattern: "t/action/result",
             defaults: new { controller = "BulkAction", action = "Result" }
         );

      }

      public void RouteTasks(IEndpointRouteBuilder routes) {

         routes.MapAreaControllerRoute(
            name: "Task Review",
            areaName: Common.ModuleName,
            pattern: "t/task/{ContentItemId}",
            defaults: new { controller = "Task", action = "Review" }
         );

         routes.MapAreaControllerRoute(
             name: "Task Review Form",
             areaName: Common.ModuleName,
             pattern: "t/task/form/{ContentItemId}",
             defaults: new { controller = "Task", action = "Form" }
         );

         routes.MapAreaControllerRoute(
             name: "Task Run",
             areaName: Common.ModuleName,
             pattern: "t/task/run/{ContentItemId}",
             defaults: new { controller = "Task", action = "Run" }
         );

         routes.MapAreaControllerRoute(
             name: "Task Run API",
             areaName: Common.ModuleName,
             pattern: "t/task/run/{format}/{ContentItemId}",
             defaults: new { controller = "Task", action = "Run", format = "json" }
         );

      }

      public void RouteReporting(IEndpointRouteBuilder routes) {

         routes.MapAreaControllerRoute(
             name: "Report Log",
             areaName: Common.ModuleName,
             pattern: "t/report/log/{ContentItemId}",
             defaults: new { controller = "Report", action = "Log" }
         );

         routes.MapAreaControllerRoute(
            name: "Stream CSV",
            areaName: Common.ModuleName,
            pattern: "t/report/stream/csv/{ContentItemId}",
            defaults: new { controller = "Report", action = "StreamCsv" }
         );

         routes.MapAreaControllerRoute(
            name: "Stream JSON",
            areaName: Common.ModuleName,
            pattern: "t/report/stream/json/{ContentItemId}",
            defaults: new { controller = "Report", action = "StreamJson" }
         );

         routes.MapAreaControllerRoute(
            name: "Stream Geo JSON",
            areaName: Common.ModuleName,
            pattern: "t/report/stream/geojson/{ContentItemId}",
            defaults: new { controller = "Report", action = "StreamGeoJson" }
         );

         routes.MapAreaControllerRoute(
            name: "Run Report API",
            areaName: Common.ModuleName,
            pattern: "t/report/{format}/{ContentItemId}",
            defaults: new { controller = "Report", action = "Run", format = "json" }
         );

         routes.MapAreaControllerRoute(
            name: "Run Report",
            areaName: Common.ModuleName,
            pattern: "t/report/{ContentItemId}",
            defaults: new { controller = "Report", action = "Index" }
         );

      }

      public void RouteCalendar(IEndpointRouteBuilder routes) {

         routes.MapAreaControllerRoute(
             name: "Report Calendar",
             areaName: Common.ModuleName,
             pattern: "t/report/calendar/{ContentItemId}",
             defaults: new { controller = "Calendar", action = "Index" }
         );

         routes.MapAreaControllerRoute(
            name: "Stream JSON to Calendar",
            areaName: Common.ModuleName,
            pattern: "t/report/stream/calendar/{ContentItemId}",
            defaults: new { controller = "Calendar", action = "Stream" }
         );

      }

      public void RouteMap(IEndpointRouteBuilder routes) {

         routes.MapAreaControllerRoute(
             name: "Report Map",
             areaName: Common.ModuleName,
             pattern: "t/report/map/{ContentItemId}",
             defaults: new { controller = "Map", action = "Index" }
         );

         routes.MapAreaControllerRoute(
            name: "Stream Geo JSON to Map",
            areaName: Common.ModuleName,
            pattern: "t/report/stream/map/{ContentItemId}",
            defaults: new { controller = "Map", action = "Stream" }
         );

      }

   }
}