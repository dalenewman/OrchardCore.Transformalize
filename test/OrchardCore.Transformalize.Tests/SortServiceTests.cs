using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformalizeModule.Services;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class SortServiceTests {

      [DataTestMethod]
      [DataRow("Name", "Namea", Direction.Asc)]
      [DataRow("Name", "Named", Direction.Desc)]
      [DataRow("Age", "Named", Direction.None)]
      [DataRow("Name", "", Direction.None)]
      public void Sort_ReturnsExpectedDirection(string src, string expression, Direction expected) {
         var service = new SortService();
         Assert.AreEqual(expected, service.Sort(src, expression));
      }

      [TestMethod]
      public void Sort_MultipleFields_ReturnsCorrectDirections() {
         var service = new SortService();
         var expression = "Namea.Aged";

         Assert.AreEqual(Direction.Asc, service.Sort("Name", expression));
         Assert.AreEqual(Direction.Desc, service.Sort("Age", expression));
         Assert.AreEqual(Direction.None, service.Sort("Other", expression));
      }

      [TestMethod]
      public void ProcessExpression_DuplicateStrippedKey_LastWriteWins() {
         // "Codea" and "Coded" both strip to "Code"; the second entry overwrites the first
         var result = SortService.ProcessExpression("Codea.Coded");
         Assert.AreEqual(1, result.Count, "duplicate stripped key should result in one entry");
         Assert.AreEqual('d', result["Code"], "last entry 'Coded' should win with descending direction");
      }
   }
}
