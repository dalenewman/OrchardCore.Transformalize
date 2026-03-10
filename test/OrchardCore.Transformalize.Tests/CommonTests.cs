using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformalizeModule;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class CommonTests {

      [DataTestMethod]
      [DataRow(".pdf", "application/pdf")]
      [DataRow("pdf", "application/pdf")]
      [DataRow("html", "text/html")]
      [DataRow("jpg", "image/jpeg")]
      [DataRow("png", "image/png")]
      [DataRow("txt", "text/plain")]
      [DataRow("css", "text/css")]
      [DataRow("", "application/octet-stream")]
      [DataRow(".xyznotreal", "application/octet-stream")]
      public void GetMimeType_ReturnsExpected(string extension, string expected) {
         Assert.AreEqual(expected, Common.GetMimeType(extension));
      }

      [DataTestMethod]
      [DataRow("pdf", true)]
      [DataRow(".pdf", true)]
      [DataRow("xyznotreal", false)]
      [DataRow("", false)]
      public void HasMimeType_ReturnsExpected(string extension, bool expected) {
         Assert.AreEqual(expected, Common.HasMimeType(extension));
      }

      [TestMethod]
      public void GetShortestUniqueVersions_DistinctFirstChars_ReturnsSingleChars() {
         var result = Common.GetShortestUniqueVersions(new[] { "Apple", "Banana", "Cherry" });
         CollectionAssert.AreEqual(new[] { "A", "B", "C" }, result);
      }

      [TestMethod]
      public void GetShortestUniqueVersions_SharedPrefixes_ExpandsAsNeeded() {
         var result = Common.GetShortestUniqueVersions(new[] { "State", "Status", "Street" });
         CollectionAssert.AreEqual(new[] { "S", "St", "Str" }, result);
      }
   }
}
