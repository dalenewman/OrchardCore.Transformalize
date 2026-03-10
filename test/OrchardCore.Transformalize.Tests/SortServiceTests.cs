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
   }
}
