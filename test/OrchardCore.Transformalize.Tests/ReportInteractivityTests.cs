using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.Modules.Services;
using OrchardCore.Title.Models;
using Transformalize.Configuration;
using Transformalize.Logging;
using TransformalizeModule.Controllers;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ReportInteractivityTests {
   [DataTestMethod]
   [DataRow(false, false, false, false)]
   [DataRow(true, false, false, true)]
   [DataRow(true, true, false, false)]
   [DataRow(true, false, true, false)]
   public async Task IndexReturnsFragmentsOnlyForReportUpdates(bool hxRequest, bool historyRestore, bool otherTarget, bool partial) {
      var report = CreateReport();
      var service = new Mock<IReportService>();
      service.Setup(s => s.Validate(It.IsAny<TransformalizeRequest>())).ReturnsAsync(report);
      var context = new DefaultHttpContext();
      context.Request.PathBase = "/tenant";
      context.Request.Path = "/t/report/demo";
      context.Request.QueryString = new QueryString("?Name=Ada&page=2");
      if (hxRequest) context.Request.Headers["HX-Request"] = "true";
      context.Request.Headers["HX-Target"] = otherTarget ? "other" : "tfl-report";
      if (historyRestore) context.Request.Headers["HX-History-Restore-Request"] = "true";
      var controller = CreateController(service, context);

      var result = await controller.Index("demo");

      if (partial) {
         Assert.IsInstanceOfType<PartialViewResult>(result);
         Assert.AreEqual("_Report", ((PartialViewResult)result).ViewName);
         Assert.AreEqual("true", context.Response.Headers["X-Transformalize-Report"].ToString());
      } else {
         Assert.IsInstanceOfType<ViewResult>(result);
      }
      var model = partial ? ((PartialViewResult)result).Model : ((ViewResult)result).Model;
      Assert.AreEqual("/tenant/t/report/demo", ((ReportViewModel)model!).ReportPath);
      Assert.AreEqual("Ada", ((ReportViewModel)model).QueryValue("Name"));
      service.Verify(s => s.RunAsync(report.Process, null, context.RequestAborted), Times.Once);
      Assert.IsTrue(context.Response.Headers.Vary.ToString().Contains("HX-History-Restore-Request"));
   }

   [TestMethod]
   public async Task EditorWithGlobalJavaScriptStillReturnsReportFragments() {
      var report = CreateReport();
      report.Process.Scripts.Add(new Script { Global = true, Language = "js", Content = "function lightbox() {}" });
      var service = new Mock<IReportService>();
      service.Setup(s => s.Validate(It.IsAny<TransformalizeRequest>())).ReturnsAsync(report);
      var context = new DefaultHttpContext();
      context.Request.Path = "/t/report/demo";
      context.Request.QueryString = new QueryString("?edit=1");
      context.Request.Headers["HX-Request"] = "true";
      context.Request.Headers["HX-Target"] = "tfl-report";

      var result = await CreateController(service, context).Index("demo");

      Assert.IsInstanceOfType<PartialViewResult>(result);
      Assert.AreEqual("1", ((ReportViewModel)((PartialViewResult)result).Model!).QueryValue("edit"));
      Assert.AreEqual("true", context.Response.Headers["X-Transformalize-Report"].ToString());
   }

   [DataTestMethod]
   [DataRow("map")]
   [DataRow("calendar")]
   public async Task DisplayModesRetainTenantPathsAndCanonicalFilters(string mode) {
      var report = CreateReport();
      report.Process.Mode = mode;
      var service = new Mock<IReportService>();
      service.Setup(s => s.Validate(It.IsAny<TransformalizeRequest>())).ReturnsAsync(report);
      var settings = new Mock<ISettingsService>();
      settings.SetupGet(s => s.Settings).Returns(new TransformalizeSettings { MapBoxToken = "fixture-token" });
      var logger = new CombinedLogger<ReportController>(NullLogger<ReportController>.Instance, new MemoryLogger(global::Transformalize.Contracts.LogLevel.Debug));
      var context = new DefaultHttpContext();
      context.Request.PathBase = "/tenant";
      context.Request.Path = $"/t/report/{mode}/demo";
      context.Request.QueryString = new QueryString("?Category=A&Category=B");
      Controller controller = mode == "map" ? new MapController(service.Object, settings.Object, logger)
         : new CalendarController(service.Object, settings.Object, logger);
      controller.ControllerContext = new ControllerContext { HttpContext = context };

      var result = mode == "map" ? await ((MapController)controller).Index("demo") : await ((CalendarController)controller).Index("demo");
      var model = (ReportViewModel)((ViewResult)result).Model!;

      Assert.AreEqual($"/tenant/t/report/{mode}/demo", model.ReportPath);
      var query = QueryHelpers.ParseQuery(model.NavigationUrl("page", "2").Split('?')[1]);
      CollectionAssert.AreEqual(new[] { "A", "B" }, query["Category"].ToArray());
      service.Verify(s => s.RunAsync(report.Process, null, context.RequestAborted), Times.Once);
   }

   [TestMethod]
   public async Task ChartsUseTheSameReportNavigationModel() {
      var model = CreateModel("?Category=A&Category=B");
      model.Process.Mode = "chart";
      var service = new Mock<IChartService>();
      service.Setup(s => s.GetReportViewModelAsync("demo")).ReturnsAsync(new ChartViewModel { ReportViewModel = model });
      var context = new DefaultHttpContext();
      context.Request.PathBase = "/tenant";
      context.Request.Path = "/t/report/chart/demo";
      var controller = new ChartController(service.Object) { ControllerContext = new ControllerContext { HttpContext = context } };

      var result = (ViewResult)await controller.Index("demo");

      Assert.AreSame(model, result.Model);
      Assert.AreEqual("/tenant/t/report/chart/demo", model.ReportPath);
      Assert.IsTrue(model.NavigationUrl("chart-type", "bar").Contains("Category=A&Category=B"));
   }

   [TestMethod]
   public void NavigationPreservesRepeatedFiltersAndTenantPath() {
      var model = CreateModel("?Category=A&Category=B&Name=A%26B&sort=0d&page=3");
      var url = model.NavigationUrl("page", "4");
      Assert.IsTrue(url.StartsWith("/tenant/t/report/demo?"));
      var query = QueryHelpers.ParseQuery(url[(url.IndexOf('?') + 1)..]);
      CollectionAssert.AreEqual(new[] { "A", "B" }, query["Category"].ToArray());
      Assert.AreEqual("A&B", query["Name"].ToString());
      Assert.AreEqual("0d", query["sort"].ToString());
      Assert.AreEqual("4", query["page"].ToString());
      Assert.IsFalse(QueryHelpers.ParseQuery(model.NavigationUrl("size", "50", true).Split('?')[1]).ContainsKey("page"));
      Assert.IsFalse(QueryHelpers.ParseQuery(model.NavigationUrl("page", "1").Split('?')[1]).ContainsKey("page"));
   }

   [TestMethod]
   public void NavigationDropsWildcardValuesFromExistingUrlsAndRetainsRealFilters() {
      var model = CreateModel("?sort=Id.d&size=25&day=*&dayofweek=%2A&hour=*&district=*&beat=*&grid=*&ucr_ncic_code=*&geojson-color=*&Category=A&Category=*&Category=B&Name=Ada%2A&Empty=");
      var query = QueryHelpers.ParseQuery(model.NavigationUrl("page", "2").Split('?')[1]);

      foreach (var name in new[] { "day", "dayofweek", "hour", "district", "beat", "grid", "ucr_ncic_code", "geojson-color", "Empty" }) {
         Assert.IsFalse(query.ContainsKey(name), name);
      }
      CollectionAssert.AreEqual(new[] { "A", "B" }, query["Category"].ToArray());
      Assert.AreEqual("Ada*", query["Name"].ToString());
      Assert.AreEqual("Id.d", query["sort"].ToString());
      Assert.AreEqual("25", query["size"].ToString());
      Assert.AreEqual("2", query["page"].ToString());
      Assert.IsFalse(QueryHelpers.ParseQuery(model.NavigationUrl("district", "*").Split('?')[1]).ContainsKey("district"));
   }

   [TestMethod]
   public async Task FailedValidationKeepsItsOriginalActionResult() {
      var report = CreateReport();
      report.Valid = false;
      report.ActionResult = new ForbidResult();
      var service = new Mock<IReportService>();
      service.Setup(s => s.Validate(It.IsAny<TransformalizeRequest>())).ReturnsAsync(report);
      var context = new DefaultHttpContext();
      context.Request.Headers["HX-Request"] = "true";
      var result = await CreateController(service, context).Index("demo");
      Assert.AreSame(report.ActionResult, result);
      service.Verify(s => s.RunAsync(It.IsAny<Process>(), It.IsAny<StreamWriter>(), It.IsAny<CancellationToken>()), Times.Never);
   }

   private static ReportController CreateController(Mock<IReportService> service, HttpContext context) =>
      new(service.Object, Mock.Of<ISlugService>(), new CombinedLogger<ReportController>(NullLogger<ReportController>.Instance, new MemoryLogger(global::Transformalize.Contracts.LogLevel.Debug)), Mock.Of<ISettingsService>(s => s.Settings == new TransformalizeSettings())) {
         ControllerContext = new ControllerContext { HttpContext = context }
      };

   private static ReportViewModel CreateModel(string query) {
      var report = CreateReport();
      var parameters = new QueryCollection(QueryHelpers.ParseQuery(query).ToDictionary(p => p.Key, p => p.Value));
      return new ReportViewModel(report.Process, report.ContentItem, parameters, "demo") { ReportPath = "/tenant/t/report/demo", Settings = new TransformalizeSettings { DefaultPageSizes = "20,25,50,100" } };
   }

   private static TransformalizeResponse<TransformalizeReportPart> CreateReport() {
      var item = new ContentItem { ContentItemId = "demo" };
      item.Weld(new TitlePart { Title = "Demo" });
      item.Weld(new TransformalizeReportPart());
      var process = new Process { Name = "demo", Mode = "report", Status = 200 };
      process.Entities.Add(new Entity { Name = "rows" });
      return new TransformalizeResponse<TransformalizeReportPart> { ContentItem = item, Process = process, Valid = true };
   }
}
