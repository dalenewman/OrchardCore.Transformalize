using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformalizeModule.Services;
using Flurl;
using Microsoft.AspNetCore.Http;
using TransformalizeModule.Models;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class LinkServiceTests {

      [DataTestMethod]
      [DataRow("25", false)]
      [DataRow("50", true)]
      [DataRow("1", false)]
      [DataRow("bad", false)]
      public void DataLinksUseTheSamePagingDefaults(string size, bool explicitSize) {
         var context = new DefaultHttpContext();
         context.Request.Scheme = "http";
         context.Request.Host = new HostString("example.test");
         context.Request.Path = "/t/report/crime";
         context.Request.QueryString = new QueryString($"?size={size}&district=7");
         var service = new LinkService(new HttpContextAccessor { HttpContext = context });

         var result = new Url(service.Create("crime", "/t/report/stream/crime", true, new ReportPaging([25, 50, 100])).ToString());

         Assert.AreEqual(explicitSize, result.QueryParams.Contains("size"));
         Assert.AreEqual("7", result.QueryParams.First(p => p.Name == "district").Value.ToString());
      }

      [TestMethod]
      public void CleaningMixedRepeatedValuesKeepsRealFilters() {
         var result = LinkService.RemoveNoiseFromUrl(new Url("https://example.test/report?Category=A&Category=*&Category=B&Empty="));
         CollectionAssert.AreEqual(new[] { "A", "B" }, result.QueryParams.Where(p => p.Name == "Category").Select(p => p.Value.ToString()).ToArray());
         Assert.IsFalse(result.QueryParams.Contains("Empty"));
      }

      [TestMethod]
      public void RemoveNoiseFromUrl_RemovesWildcardAndEmptyParams() {
         var url = new Url("https://example.com/report?name=John&status=*&empty=&age=30");

         var result = LinkService.RemoveNoiseFromUrl(url);

         Assert.IsFalse(result.QueryParams.Contains("status"), "wildcard (*) param should be removed");
         Assert.IsFalse(result.QueryParams.Contains("empty"), "empty param should be removed");
         Assert.IsTrue(result.QueryParams.Contains("name"), "non-empty param should be kept");
         Assert.IsTrue(result.QueryParams.Contains("age"), "non-empty param should be kept");
         Assert.AreEqual("https://example.com/report?name=John&age=30", result.ToString(), "only clean params remain");
      }

      [TestMethod]
      public void RemoveNoiseFromUrl_LeavesCleanUrlAlone() {
         var url = new Url("https://example.com/report?name=John&age=30");

         var result = LinkService.RemoveNoiseFromUrl(url);

         Assert.AreEqual("https://example.com/report?name=John&age=30", result.ToString(), "clean URL should be unchanged");
      }
   }
}
