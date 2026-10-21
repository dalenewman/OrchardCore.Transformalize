using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class FormPlacesTests {
   [TestMethod]
   public void ComponentTargetsPreserveAliasesAndFirstMatchPrecedence() {
      var process = new Process();
      process.Maps.Add(new() { Name = "address" });
      var map = process.Maps[0];
      map.Items.Add(new() { From = "street", To = "Street" });
      map.Items.Add(new() { From = "route", To = "IgnoredStreet" });
      map.Items.Add(new() { From = "city", To = "City" });
      map.Items.Add(new() { From = "county", To = "LegacyLevel1" });
      map.Items.Add(new() { From = "state", To = "LegacyLevel2" });
      map.Items.Add(new() { From = "country", To = "Country" });
      var model = new FormPlacesViewModel { Process = process, Parameter = new Parameter { Name = "Search", Map = "address" } };
      var targets = model.ComponentTargets;
      Assert.AreEqual("Street", targets["route"]);
      Assert.AreEqual("City", targets["locality"]);
      Assert.AreEqual("LegacyLevel1", targets["administrative_area_level_1"]);
      Assert.AreEqual("LegacyLevel2", targets["administrative_area_level_2"]);
      Assert.AreEqual("Country", targets["country"]);
      Assert.IsFalse(targets.ContainsKey("postal_code"));
   }

   [TestMethod]
   public void MissingMapsAndUnknownComponentsDoNotInventTargets() {
      var process = new Process();
      var model = new FormPlacesViewModel { Process = process, Parameter = new Parameter { Name = "Search", Map = "address" } };
      Assert.AreEqual(0, model.ComponentTargets.Count);
      process.Maps.Add(new() { Name = "address" });
      process.Maps[0].Items.Add(new() { From = "unknown", To = "Other" });
      process.Maps[0].Items.Add(new() { From = "route", To = string.Empty });
      Assert.AreEqual(0, model.ComponentTargets.Count);
   }
}
