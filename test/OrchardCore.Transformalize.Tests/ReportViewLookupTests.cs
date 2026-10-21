using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformalizeModule.Controllers;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ReportViewLookupTests {
   [DataTestMethod]
   [DataRow("Report")]
   [DataRow("Chart")]
   [DataRow("Map")]
   [DataRow("Calendar")]
   public async Task SharedShellAndNestedPartialsResolveFromEveryReportController(string controller) {
      var assembly = typeof(ReportController).Assembly;
      var compiledViews = new RazorCompiledItemLoader().LoadItems(assembly);
      var shell = compiledViews.Single(view => view.Identifier.EndsWith("/Views/Shared/Report.cshtml", StringComparison.Ordinal));
      var moduleRoot = shell.Identifier[..shell.Identifier.IndexOf("/Views/", StringComparison.Ordinal)];
      var builder = WebApplication.CreateBuilder();
      builder.Services.AddControllersWithViews().AddApplicationPart(assembly);
      builder.Services.Configure<RazorViewEngineOptions>(options => {
         // Orchard modules prefix their compiled views with the module name.
         options.AreaViewLocationFormats.Insert(0, moduleRoot + "/Views/{1}/{0}.cshtml");
         options.AreaViewLocationFormats.Insert(1, moduleRoot + "/Views/Shared/{0}.cshtml");
      });
      await using var app = builder.Build();
      using var scope = app.Services.CreateScope();
      var route = new RouteData();
      route.Values["controller"] = controller;
      route.Values["area"] = TransformalizeModule.Common.ModuleName;
      var context = new ActionContext(new DefaultHttpContext { RequestServices = scope.ServiceProvider }, route,
         new ActionDescriptor { RouteValues = new Dictionary<string, string?> {
            ["controller"] = controller, ["area"] = TransformalizeModule.Common.ModuleName
         } });
      var engine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();

      foreach (var name in new[] { "Report", "_Report", "_ReportTables", "Parameters", "Controls", "_MapDisplay", "_CalendarDisplay", "Chart", "_ReportModal", "_ArrangementResources" }) {
         var result = engine.FindView(context, name, isMainPage: false);
         Assert.IsTrue(result.Success, $"{controller} could not find {name}: {string.Join(", ", result.SearchedLocations ?? [])}");
      }
   }
}
