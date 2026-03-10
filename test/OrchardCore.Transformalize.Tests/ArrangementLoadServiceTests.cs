using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.Services;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class ArrangementLoadServiceTests {

      [TestMethod]
      public void EnforcePageSize_ClampsOversizedPageToMax() {
         var process = CreateProcessWithOneEntity();
         var parameters = new Dictionary<string, string> { { "size", "99999" }, { "page", "1" } };

         ArrangementLoadService.EnforcePageSize(process, parameters, min: 10, chosen: 99999, max: 100);

         Assert.AreEqual(100, process.Entities[0].Size, "size should be clamped to max");
      }

      [TestMethod]
      public void EnforcePageSize_ZeroSizeDefaultsToMin() {
         var process = CreateProcessWithOneEntity();
         var parameters = new Dictionary<string, string> { { "size", "0" }, { "page", "1" } };

         ArrangementLoadService.EnforcePageSize(process, parameters, min: 10, chosen: 0, max: 100);

         Assert.AreEqual(10, process.Entities[0].Size, "zero size should fall back to min");
      }

      [DataTestMethod]
      [DataRow("abc", 1, DisplayName = "Invalid page defaults to 1")]
      [DataRow("0", 1, DisplayName = "Zero page becomes 1")]
      [DataRow("5", 5, DisplayName = "Valid page is preserved")]
      public void EnforcePageSize_PageParsing(string pageValue, int expectedPage) {
         var process = CreateProcessWithOneEntity();
         var parameters = new Dictionary<string, string> { { "page", pageValue } };

         ArrangementLoadService.EnforcePageSize(process, parameters, min: 10, chosen: 20, max: 100);

         Assert.AreEqual(expectedPage, process.Entities[0].Page);
      }

      [TestMethod]
      public void EnforcePageSize_AppliesToAllEntities() {
         var process = new Process();
         process.Entities.Add(new Entity { Name = "A" });
         process.Entities.Add(new Entity { Name = "B" });
         var parameters = new Dictionary<string, string> { { "page", "3" }, { "size", "25" } };

         ArrangementLoadService.EnforcePageSize(process, parameters, min: 10, chosen: 25, max: 100);

         foreach (var entity in process.Entities) {
            Assert.AreEqual(3, entity.Page, "page should be applied to every entity");
            Assert.AreEqual(25, entity.Size, "size should be applied to every entity");
         }
      }

      [TestMethod]
      public void ConfineData_DisablesUnrequiredFields_RenamesRequired() {
         var process = new Process();
         var entity = new Entity { Name = "Test" };
         entity.Fields.Add(new Field { Name = "LatField", Alias = "LatField", Output = true, Input = true });
         entity.Fields.Add(new Field { Name = "Extra", Alias = "Extra", Output = true, Input = true });
         process.Entities.Add(entity);

         var required = new Dictionary<string, string> { { "LatField", "latitude" } };

         ArrangementLoadService.ConfineData(process, required);

         var lat = entity.Fields.First(f => f.Name == "LatField");
         var extra = entity.Fields.First(f => f.Name == "Extra");

         Assert.AreEqual("latitude", lat.Alias, "Required field should be renamed");
         Assert.IsTrue(lat.Output, "Required field should stay output");
         Assert.IsFalse(extra.Output, "Unrequired field should be disabled");
         Assert.IsFalse(extra.Input, "Unrequired field should be disabled");
      }

      [TestMethod]
      public void ConfineData_PropertyFieldsAreKept() {
         var process = new Process();
         var entity = new Entity { Name = "Test" };
         entity.Fields.Add(new Field { Name = "Required", Alias = "Required", Output = false });
         entity.Fields.Add(new Field { Name = "Prop", Alias = "Prop", Output = false, Property = true });
         entity.Fields.Add(new Field { Name = "Extra", Alias = "Extra", Output = true });
         process.Entities.Add(entity);

         ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { { "Required", "Required" } });

         Assert.IsTrue(entity.Fields.First(f => f.Name == "Prop").Output, "fields marked Property should be kept in output");
      }

      private static Process CreateProcessWithOneEntity() {
         var process = new Process();
         process.Entities.Add(new Entity { Name = "Test" });
         return process;
      }
   }
}
