using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrchardCore.ResourceManagement;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ReportResourceTests {
   [TestMethod]
   public void ModernReportsLoadScopedFiltersAndCalendarsWithoutJQueryAdapters() {
      var options = new ResourceManagementOptions();
      new TransformalizeModule.ResourceManagementOptionsConfiguration().Configure(options);
      var manager = new ResourceManager(Options.Create(options), null!);
      manager.RegisterResource("script", "tfl-report-interactivity").AtFoot();
      var names = manager.GetRequiredResources("script").Select(r => r.Resource.Name).ToList();
      foreach (var name in new[] { "tfl-htmx", "tfl-stimulus", "tfl-report-filter", "tfl-flatpickr", "tfl-report-controller" }) {
         CollectionAssert.Contains(names, name);
      }
      foreach (var name in new[] { "jQuery", "jQuery-ui", "bootstrap-select", "bootstrap-select-beta3", "tfl-report" }) {
         CollectionAssert.DoesNotContain(names, name);
      }
      Assert.IsTrue(names.IndexOf("tfl-report-filter") < names.IndexOf("tfl-report-interactivity"));
   }
}
