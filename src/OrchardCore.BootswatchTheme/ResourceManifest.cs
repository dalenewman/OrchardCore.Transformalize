using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;
using OrchardCore.BootswatchTheme.Settings;

namespace OrchardCore.BootswatchTheme {
   public sealed class ResourceManifestConfiguration : IConfigureOptions<ResourceManagementOptions> {
      private static readonly ResourceManifest _manifest;

      static ResourceManifestConfiguration() {
         _manifest = new ResourceManifest();

         foreach (var theme in Constants.BootswatchThemes) {
            var style = _manifest
               .DefineStyle($"bootswatch-{theme}")
               .SetUrl(
                  $"~/BootswatchTheme/styles/{theme}/bootstrap.min.css",
                  $"~/BootswatchTheme/styles/{theme}/bootstrap.css")
               .SetVersion(theme == Constants.CodexTheme ? "5.3.8" : "5.0");

            // Codex is our own bundled style, not a theme published by Bootswatch.
            // Leave its CDN URL unset so UseCdn also serves the local asset.
            if (theme != Constants.CodexTheme) {
               style.SetCdn(
                  $"https://cdn.jsdelivr.net/npm/bootswatch@5/dist/{theme}/bootstrap.min.css",
                  $"https://cdn.jsdelivr.net/npm/bootswatch@5/dist/{theme}/bootstrap.css");
            }
         }

         _manifest.DefineStyle("bootswatch-codex-rtl")
            .SetUrl("~/BootswatchTheme/styles/codex/bootstrap.rtl.min.css",
               "~/BootswatchTheme/styles/codex/bootstrap.rtl.css")
            .SetVersion("5.3.8");
      }

      public void Configure(ResourceManagementOptions options) {
         options.ResourceManifests.Add(_manifest);
      }
   }
}
