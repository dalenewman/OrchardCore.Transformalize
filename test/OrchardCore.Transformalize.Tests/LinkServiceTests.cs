using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformalizeModule.Services;
using Flurl;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class LinkServiceTests {

      [TestMethod]
      public void RemoveNoiseFromUrl_RemovesWildcardAndEmptyParams() {
         var url = new Url("https://example.com/t/report?name=John&status=*&category=&age=30");

         var result = LinkService.RemoveNoiseFromUrl(url);

         Assert.IsFalse(result.QueryParams.Contains("status"), "wildcard (*) param should be removed");
         Assert.IsFalse(result.QueryParams.Contains("category"), "empty param should be removed");
         Assert.IsTrue(result.QueryParams.Contains("name"), "non-empty param should be kept");
         Assert.IsTrue(result.QueryParams.Contains("age"), "non-empty param should be kept");
         Assert.AreEqual("https://example.com/t/report?name=John&age=30", result.ToString());
      }

      [TestMethod]
      public void RemoveNoiseFromUrl_LeavesCleanUrlAlone() {
         var url = new Url("https://example.com/t/report?name=John&age=30");
         Assert.AreEqual("https://example.com/t/report?name=John&age=30", LinkService.RemoveNoiseFromUrl(url).ToString());
      }
   }
}
