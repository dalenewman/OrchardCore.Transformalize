using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.Controllers;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class ReportControllerTests {

      // default field names that match TransformalizeReportPart defaults
      private static readonly HashSet<string> DefaultSuppress    = new() { "batchvalue", "geojson-color", "geojson-description" };
      private static readonly HashSet<string> DefaultCoordinates = new() { "latitude", "longitude" };

      // ── ApplyGeoJsonFieldVisibility ──────────────────────────────────────

      [TestMethod]
      public void ApplyGeoJsonFieldVisibility_SuppressedFields_AreHiddenAndRenamed() {
         var entity = BuildEntity(
            ("batchvalue",        output: true,  property: true,  export: "defer"),
            ("geojson-color",     output: true,  property: false, export: "defer"),
            ("latitude",          output: true,  property: false, export: "defer"),
            ("title",             output: true,  property: false, export: "defer")
         );

         ReportController.ApplyGeoJsonFieldVisibility(new[] { entity }, DefaultSuppress, DefaultCoordinates);

         var batch = entity.Fields.First(f => f.Name == "batchvalue");
         Assert.IsFalse(batch.Output,   "batchvalue should be suppressed");
         Assert.IsFalse(batch.Property, "batchvalue should lose Property");
         Assert.AreEqual("batchvalueSuppressed", batch.Alias, "suppressed alias gets 'Suppressed' suffix");

         var color = entity.Fields.First(f => f.Name == "geojson-color");
         Assert.IsFalse(color.Output, "geojson-color should be suppressed");

         var lat = entity.Fields.First(f => f.Name == "latitude");
         Assert.IsFalse(lat.Property, "coordinate field with Export=defer should have Property=false");

         var title = entity.Fields.First(f => f.Name == "title");
         Assert.IsTrue(title.Property, "regular field with Output=true and Export=defer should get Property=true");
      }

      [DataTestMethod]
      [DataRow("true",  true,  DisplayName = "coordinate with Export=true gets Property=true")]
      [DataRow("defer", false, DisplayName = "coordinate with Export=defer gets Property=false")]
      [DataRow("false", false, DisplayName = "coordinate with Export=false gets Property=false")]
      public void ApplyGeoJsonFieldVisibility_CoordinateField_PropertyFollowsExport(string export, bool expectedProperty) {
         var entity = BuildEntity(("latitude", output: true, property: true, export: export));
         ReportController.ApplyGeoJsonFieldVisibility(new[] { entity }, new HashSet<string>(), new HashSet<string> { "latitude" });
         Assert.AreEqual(expectedProperty, entity.Fields[0].Property, $"latitude with Export={export}");
      }

      [DataTestMethod]
      [DataRow(true,  "defer", true,  DisplayName = "Property=true stays true regardless of export")]
      [DataRow(false, "defer", true,  DisplayName = "Output=true + Export=defer makes Property true")]
      [DataRow(false, "true",  true,  DisplayName = "Export=true makes Property true")]
      [DataRow(false, "false", false, DisplayName = "Output=true + Export=false leaves Property false")]
      public void ApplyGeoJsonFieldVisibility_RegularField_PropertyLogic(bool initialProperty, string export, bool expectedProperty) {
         var entity = BuildEntity(("title", output: true, property: initialProperty, export: export));
         ReportController.ApplyGeoJsonFieldVisibility(new[] { entity }, new HashSet<string>(), new HashSet<string>());
         Assert.AreEqual(expectedProperty, entity.Fields[0].Property, $"Property={initialProperty}, Export={export}");
      }

      // ── helpers ───────────────────────────────────────────────────────────

      private static Entity BuildEntity(params (string name, bool output, bool property, string export)[] fields) {
         var entity = new Entity { Name = "report" };
         foreach (var (name, output, property, export) in fields)
            entity.Fields.Add(new Field { Name = name, Alias = name, Output = output, Property = property, Export = export });
         return entity;
      }
   }
}
