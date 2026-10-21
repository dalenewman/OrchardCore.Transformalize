using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrchardCore.ContentManagement;
using OrchardCore.Title.Models;
using Transformalize.Configuration;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ReportPagingTests {
   [DataTestMethod]
   [DataRow(null, 25, "")]
   [DataRow("25", 25, "")]
   [DataRow("50", 50, "50")]
   [DataRow("1", 25, "")]
   [DataRow("0", 25, "")]
   [DataRow("-1", 25, "")]
   [DataRow("75", 25, "")]
   [DataRow("invalid", 25, "")]
   [DataRow("10000", 100, "100")]
   public void ConfiguredSizesControlFetchingAndUrlState(string? requested, int expected, string parameter) {
      var paging = ReportPaging.For(new TransformalizeReportPart(), Settings(), "report");
      var process = new Process { Entities = [new Entity { Name = "rows", Size = 1 }] };
      var parameters = new Dictionary<string, string>();
      if (requested != null) parameters["size"] = requested;

      ArrangementLoadService.EnforcePageSize(process, parameters, paging);

      Assert.AreEqual(expected, process.Entities[0].Size);
      Assert.AreEqual(parameter, paging.SizeParameter(requested));
      Assert.AreEqual(25, paging.DefaultSize);
   }

   [DataTestMethod]
   [DataRow("", 25, false)]
   [DataRow("?size=25", 25, false)]
   [DataRow("?size=1", 25, false)]
   [DataRow("?size=50", 50, true)]
   public void ChartFetchSizeNeverBecomesUserPagingState(string query, int expectedSize, bool explicitSize) {
      var model = Model(query, "chart");
      model.Process.Entities[0].Size = 1; // The chart loader intentionally fetches one row.

      Assert.AreEqual(expectedSize, model.PageSize);
      var filtered = QueryHelpers.ParseQuery(model.NavigationUrl("district", "7").Split('?')[1]);
      Assert.AreEqual(explicitSize, filtered.ContainsKey("size"));
      if (explicitSize) Assert.AreEqual("50", filtered["size"].ToString());

      var returned = QueryHelpers.ParseQuery(model.ViewUrl("/tenant/t/report/crime", "report").Split('?').ElementAtOrDefault(1));
      Assert.AreEqual(explicitSize, returned.ContainsKey("size"));
      Assert.IsFalse(returned.Values.Any(values => values.ToString() == "1"));
   }

   [TestMethod]
   public void ChartRoundTripKeepsFiltersAndSelectedSizeButResetsModeSpecificState() {
      var table = Model("?Category=A&Category=B&size=50&page=3", "report");
      var chartUrl = table.ViewUrl("/tenant/t/report/chart/crime", "chart");
      Assert.IsFalse(QueryHelpers.ParseQuery(chartUrl.Split('?')[1]).ContainsKey("page"));
      var chart = Model("?Category=A&Category=B&size=50&page=0&chart-type=bar&chart-legend=false&chart-pct=true", "chart");
      chart.Process.Entities[0].Size = 1;
      var returned = QueryHelpers.ParseQuery(chart.ViewUrl("/tenant/t/report/crime", "report").Split('?')[1]);
      CollectionAssert.AreEqual(new[] { "A", "B" }, returned["Category"].ToArray());
      Assert.AreEqual("50", returned["size"].ToString());
      foreach (var name in new[] { "page", "chart-type", "chart-legend", "chart-pct" }) Assert.IsFalse(returned.ContainsKey(name), name);
   }

   [TestMethod]
   public void ReportOverridesAndInheritedModuleDefaultsUseTheSamePolicy() {
      var model = Model("?size=25", "report");
      Assert.IsFalse(model.NavigationUrl("page", "2").Contains("size="));
      model.Part.PageSizes.Text = "40,80";
      Assert.AreEqual(40, model.PageSize);
      Assert.IsFalse(model.NavigationUrl("page", "2").Contains("size="));
      Assert.IsTrue(model.NavigationUrl("size", "80").Contains("size=80"));
      model.Process.Mode = "chart";
      model.Part.PageSizesExtended.Text = "0";
      Assert.AreEqual(40, model.PageSize);
      Assert.IsFalse(model.Paging.FetchPaged);
   }

   [TestMethod]
   public void UnpagedVisualizationsKeepThePreferenceForReturningToATable() {
      var model = Model("?size=50&page=0", "map");
      model.Part.PageSizesExtended.Text = "0";
      Assert.IsFalse(model.Paging.FetchPaged);
      Assert.AreEqual(50, model.PageSize);
      var returned = QueryHelpers.ParseQuery(model.ViewUrl("/tenant/t/report/crime", "report").Split('?')[1]);
      Assert.AreEqual("50", returned["size"].ToString());
      Assert.IsFalse(returned.ContainsKey("page"));
   }

   [TestMethod]
   public void DisabledPagingDoesNotExposeAnInternalEntitySize() {
      var model = Model("?size=1", "chart");
      model.Part.PageSizes.Text = "0";
      Assert.IsFalse(model.Paging.Enabled);
      Assert.AreEqual(0, model.PageSize);
      Assert.IsFalse(model.ViewUrl("/tenant/t/report/crime", "report").Contains("size="));
   }

   private static TransformalizeSettings Settings() => new() { DefaultPageSizes = "25,50,100" };

   private static ReportViewModel Model(string query, string mode) {
      var item = new ContentItem();
      item.Weld(new TitlePart { Title = "Crime" });
      item.Weld(new TransformalizeReportPart());
      return new ReportViewModel(new Process { Mode = mode, Entities = [new Entity { Name = "rows", Page = 1, Size = 1 }] },
         item, new QueryCollection(QueryHelpers.ParseQuery(query).ToDictionary(pair => pair.Key, pair => pair.Value)), "crime") {
         Settings = Settings(), ReportPath = mode == "report" ? "/tenant/t/report/crime" : $"/tenant/t/report/{mode}/crime"
      };
   }
}
