using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrchardCore.ResourceManagement;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class UploadResourceTests {
   [TestMethod]
   public void UploadAdapterLoadsOneCoreWithProcessingAndImageExtensionsInOrder() {
      var options = new ResourceManagementOptions();
      new TransformalizeModule.ResourceManagementOptionsConfiguration().Configure(options);
      // Orchard also registers this name at a newer version. An unversioned
      // dependency must not reload the core after our extensions in the head.
      var orchard = new ResourceManifest();
      orchard.DefineScript("jQuery").SetVersion("3.7.1");
      orchard.DefineScript("jQuery-ui").SetDependencies("jQuery").SetVersion("1.14.2");
      orchard.DefineScript("jquery-fileupload").SetVersion("10.32.0").SetDependencies("jQuery-ui");
      options.ResourceManifests.Add(orchard);
      var manager = new ResourceManager(Options.Create(options), null!);
      manager.RegisterResource("script", "jquery-fileupload").UseVersion("10.31.0").AtHead();
      manager.RegisterResource("script", "jquery-fileupload-process").UseVersion("10.31.0").AtHead();
      manager.RegisterResource("script", "jquery-fileupload-image").UseVersion("10.31.0").AtHead();
      manager.RegisterResource("script", "tfl-form-upload-widgets").AtFoot();

      var resources = manager.GetRequiredResources("script").Select(r => r.Resource).ToList();
      var cores = resources.Where(r => r.Name == "jquery-fileupload").ToList();
      Assert.AreEqual(1, cores.Count, "Reloading the core removes the image-processing methods.");
      Assert.AreEqual("10.31.0", cores[0].Version);
      var names = resources.Select(r => r.Name).ToList();
      Assert.IsTrue(names.IndexOf("jQuery-ui") < names.IndexOf("jquery-fileupload"));
      Assert.IsTrue(names.IndexOf("jquery-fileupload") < names.IndexOf("jquery-fileupload-process"));
      Assert.IsTrue(names.IndexOf("jquery-fileupload-process") < names.IndexOf("jquery-fileupload-image"));
      Assert.IsTrue(names.IndexOf("jquery-fileupload-image") < names.IndexOf("tfl-form-upload-widgets"));
   }
}
