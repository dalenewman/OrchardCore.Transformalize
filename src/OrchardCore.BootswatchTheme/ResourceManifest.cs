using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;
using OrchardCore.BootswatchTheme.Settings;

namespace OrchardCore.BootswatchTheme {
   public sealed class ResourceManifestConfiguration : IConfigureOptions<ResourceManagementOptions> {
      private static readonly ResourceManifest _manifest;

      static ResourceManifestConfiguration() {
         _manifest = new ResourceManifest();

         foreach (var theme in Constants.BootswatchThemes) {
            _manifest
               .DefineStyle($"bootswatch-{theme}")
               .SetUrl(
                  $"~/BootswatchTheme/styles/{theme}/bootstrap.min.css",
                  $"~/BootswatchTheme/styles/{theme}/bootstrap.css")
               .SetCdn(
                  $"https://cdn.jsdelivr.net/npm/bootswatch@5/dist/{theme}/bootstrap.min.css",
                  $"https://cdn.jsdelivr.net/npm/bootswatch@5/dist/{theme}/bootstrap.css")
               .SetVersion("5.0");
         }
      }

      public void Configure(ResourceManagementOptions options) {
         options.ResourceManifests.Add(_manifest);
      }
   }
}
