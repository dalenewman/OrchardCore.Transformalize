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

   [DataTestMethod]
   [DataRow("?edit=1", false)]
   [DataRow("", true)]
   public void EditingUsesLegacyBehavior(string query, bool interactive) {
      Assert.AreEqual(interactive, CreateModel(query).InteractiveReport);
   }

   [TestMethod]
   public void ArrangementJavaScriptUsesLegacyBehavior() {
      var model = CreateModel("");
      model.Process.Scripts.Add(new Script { Global = true, Language = "js", Content = "custom()" });
      Assert.IsFalse(model.InteractiveReport);
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
      new(service.Object, Mock.Of<ISlugService>(), new CombinedLogger<ReportController>(NullLogger<ReportController>.Instance, new MemoryLogger(global::Transformalize.Contracts.LogLevel.Debug))) {
         ControllerContext = new ControllerContext { HttpContext = context }
      };

   private static ReportViewModel CreateModel(string query) {
      var report = CreateReport();
      var parameters = new QueryCollection(QueryHelpers.ParseQuery(query).ToDictionary(p => p.Key, p => p.Value));
      return new ReportViewModel(report.Process, report.ContentItem, parameters, "demo") { ReportPath = "/tenant/t/report/demo" };
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
