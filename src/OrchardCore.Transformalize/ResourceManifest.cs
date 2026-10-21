using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace TransformalizeModule {

   public class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions> {

      private static readonly ResourceManifest _manifest;

      static ResourceManagementOptionsConfiguration() {
         _manifest = new ResourceManifest();

         _manifest.DefineScript("tfl-htmx")
            .SetUrl($"~/{Common.ModuleName}/Scripts/htmx.js")
            .SetVersion("2.0.11");
         _manifest.DefineScript("tfl-stimulus")
            .SetUrl($"~/{Common.ModuleName}/Scripts/stimulus.umd.js")
            .SetVersion("3.2.2");
         _manifest.DefineScript("tfl-report-widgets")
            .SetUrl($"~/{Common.ModuleName}/Scripts/report-widgets.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-date-widgets");
         _manifest.DefineScript("tfl-report-filter")
            .SetUrl($"~/{Common.ModuleName}/Scripts/report-filter-controller.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-stimulus");
         _manifest.DefineScript("tfl-report-controller")
            .SetUrl($"~/{Common.ModuleName}/Scripts/report-controller.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.1.0")
            .SetDependencies("tfl-stimulus", "tfl-report-widgets");
         foreach (var controller in new[] { "selection", "modal", "clipboard", "editor" }) {
            _manifest.DefineScript($"tfl-report-{controller}")
               .SetUrl($"~/{Common.ModuleName}/Scripts/report-{controller}-controller.js")
               .ShouldAppendVersion(true)
               .SetVersion("1.0.0")
               .SetDependencies("tfl-report-controller");
         }
         _manifest.DefineScript("tfl-report-interactivity")
            .SetUrl($"~/{Common.ModuleName}/Scripts/report-interactivity.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.1.0")
            .SetDependencies("tfl-htmx", "tfl-report-filter", "tfl-report-selection", "tfl-report-modal", "tfl-report-clipboard", "tfl-report-editor");
         _manifest.DefineStyle("tfl-report")
            .SetUrl($"~/{Common.ModuleName}/Styles/report.css")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0");

         _manifest.DefineScript("tfl-flatpickr")
            .SetUrl($"~/{Common.ModuleName}/Scripts/flatpickr.js")
            .SetVersion("4.6.13");
         _manifest.DefineStyle("tfl-flatpickr")
            .SetUrl($"~/{Common.ModuleName}/Styles/flatpickr.css")
            .SetVersion("4.6.13");
         _manifest.DefineScript("tfl-date-widgets")
            .SetUrl($"~/{Common.ModuleName}/Scripts/date-widgets.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-flatpickr");
         _manifest.DefineScript("tfl-form-widgets")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-widgets.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-date-widgets");
         _manifest.DefineScript("tfl-form-controller")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-controller.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-stimulus", "tfl-form-widgets");
         foreach (var widget in new[] { "map", "location", "places" }) {
            _manifest.DefineScript($"tfl-form-{widget}")
               .SetUrl($"~/{Common.ModuleName}/Scripts/form-{widget}-controller.js")
               .ShouldAppendVersion(true)
               .SetVersion("1.0.0")
               .SetDependencies(widget == "places" ? new[] { "tfl-stimulus", "tfl-form-places-widgets" } : new[] { "tfl-stimulus" });
         }
         _manifest.DefineScript("tfl-form-upload-widgets")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-upload-widgets.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            // An unversioned core dependency resolves Orchard's newer registration
            // and reloads the widget after its processing/image extensions.
            .SetDependencies("jquery-fileupload-image:10.31.0");
         _manifest.DefineScript("tfl-form-upload")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-upload-controller.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-stimulus");
         _manifest.DefineScript("tfl-form-places-widgets")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-places-widgets.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0");
         _manifest.DefineScript("tfl-form-interactivity")
            .SetUrl($"~/{Common.ModuleName}/Scripts/form-interactivity.js")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0")
            .SetDependencies("tfl-htmx", "tfl-form-controller", "tfl-form-map", "tfl-form-location", "tfl-form-upload", "tfl-form-places");
         _manifest.DefineStyle("tfl-form")
            .SetUrl($"~/{Common.ModuleName}/Styles/form.css")
            .ShouldAppendVersion(true)
            .SetVersion("1.0.0");

         /* CodeMirror 6 — pre-built IIFE bundle (see cm6-build/ for source) */
         _manifest
             .DefineScript("codemirror6")
             .SetUrl(
                 $"~/{Common.ModuleName}/Scripts/codemirror6-bundle.min.js",
                 $"~/{Common.ModuleName}/Scripts/codemirror6-bundle.js")
             .SetVersion(Common.CodeMirrorVersion);

         // Prismjs
         _manifest
             .DefineScript("prism")
             .SetUrl($"~/{Common.ModuleName}/Scripts/prismjs/prism.min.js", $"~/{Common.ModuleName}/Scripts/prismjs/prism.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/prism.min.js", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/prism.js")
             .SetCdnIntegrity("sha256-3teItwIfMuVB74Alnxw/y5HAZ2irOsCULFff3EgbtEs=", "sha256-ooBCsFqD8/ih41/S1sKjWdtTyGWUuetgVvUWRF1CbN0=")
             .SetVersion("1.20.0");

         _manifest
             .DefineScript("prism-markup")
             .SetUrl($"~/{Common.ModuleName}/Scripts/prismjs/components/prism-markup.min.js", $"~/{Common.ModuleName}/Scripts/prismjs/components/prism-markup.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/components/prism-markup.min.js", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/components/prism-markup.js")
             .SetCdnIntegrity("sha256-Jp/hNSrSnxiyz2YWkA6uZSI7FMULRHemA0yA/5BhEdU=", "sha256-n9xE+cin+LSEDenLFLMs1i+Q/EidigipCXNw/Hx+4Ys=")
             .SetVersion("1.20.0");

         _manifest
             .DefineStyle("prism")
             .SetUrl($"~/{Common.ModuleName}/Styles/prismjs/prism.min.css", $"~/{Common.ModuleName}/Styles/prismjs/prism.min.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/themes/prism.min.css", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/themes/prism.css")
             .SetCdnIntegrity("sha256-cuvic28gVvjQIo3Q4hnRpQSNB0aMw3C+kjkR0i+hrWg=", "sha256-LArq5Cddn4QETLgVbBZppSkBLJbL1dhacPVQF/EPH00=")
             .SetVersion("1.20.0");

         // mapbox-gl
         _manifest
            .DefineScript("mapbox-gl")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl/3.6.0/mapbox-gl.min.js", "https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl/3.6.0/mapbox-gl.js")
            .SetCdnIntegrity("sha256-vX/UjDfsGzuQ3yOalPSb5LsgTtPsMOMXJDkEdHNZN90=", "sha256-El6IPTmbQmUpQGSfohIZ/EJAwtBHjMVivKDENsRYR2I=")
            .SetUrl($"~/{Common.ModuleName}/Scripts/mapbox-gl.min.js", $"~/{Common.ModuleName}/Scripts/mapbox-gl.js")
            .SetVersion("3.6.0");

         _manifest
             .DefineStyle("mapbox-gl")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl/3.6.0/mapbox-gl.min.css", "https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl/3.6.0/mapbox-gl.css")
             .SetCdnIntegrity("sha256-IF6XQEns8/1a1aGEcAm/L5jJLZuFQxSYB9BusB5zxFk=", "sha256-phBoVBJfBh4yTgoQQfJaE8JpuQFnXiQNcRMuYywgINw=")
             .SetUrl($"~/{Common.ModuleName}/Styles/mapbox-gl.min.css", $"~/{Common.ModuleName}/Styles/mapbox-gl.css")
             .SetVersion("3.6.0");

         _manifest
            .DefineScript("mapbox-gl-draw")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl-draw/1.4.3/mapbox-gl-draw.js", "https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl-draw/1.4.3/mapbox-gl-draw-unminified.js")
            .SetCdnIntegrity("sha256-FD4+Xf03zRK5hC+WbuacQlCZctfkC20j0wgoxNy48pI=", "sha256-CBIeMLsftxcKtFXkOg5DEWOl2DJTQ49Ie9AQ3heWqys=")
            .SetUrl($"~/{Common.ModuleName}/Scripts/mapbox-gl-draw.min.js", $"~/{Common.ModuleName}/Scripts/mapbox-gl-draw.js")
            .SetVersion("1.4.3");

         _manifest
             .DefineStyle("mapbox-gl-draw")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl-draw/1.4.3/mapbox-gl-draw.min.css", "https://cdnjs.cloudflare.com/ajax/libs/mapbox-gl-draw/1.4.3/mapbox-gl-draw.css")
             .SetCdnIntegrity("sha256-kRyG3UPDXf0G8tGwgWFV7mdVu+EkzeItDSQQF745xsc=", "sha256-17tTeM3yuBswUqyCE0vegMfNFyPAQk2mLxjdOYNFv7U=")
             .SetUrl($"~/{Common.ModuleName}/Styles/mapbox-gl-draw.min.css", $"~/{Common.ModuleName}/Styles/mapbox-gl-draw.css")
             .SetVersion("1.4.3");

         _manifest
            .DefineScript("turf")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/Turf.js/6.5.0/turf.min.js")
            .SetCdnIntegrity("sha256-0A8+j/io+cED2tYcL9S7WBQ+FASq398J4pttsaLeCj8=")
            .SetUrl($"~/{Common.ModuleName}/Scripts/turf.min.js")
            .SetVersion("6.5.0");

         _manifest
            .DefineScript("bootstrap-calendar")
            .SetUrl($"~/{Common.ModuleName}/Scripts/calendar.min.js", $"~/{Common.ModuleName}/Scripts/calendar.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-calendar/0.2.5/js/calendar.min.js", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-calendar/0.2.5/js/calendar.js")
            .SetCdnIntegrity("sha512-1tpLJ4USZ1V/MWwEfikVjpCMnKv9uy0jVWs+jX7Hl9Uca129P9lrFXyBcMx2ckIm6MM1MlELeK2dLIo5nHhP7A==", "sha512-oXhFBLw3n73Zt5berP4kOmKksbaFi4NdO4/yoDJid4u5zk8vmOQVZ0QTCGh95L+hsKMgt7d31KqmSm3FdRO9kQ==")
            .SetVersion("0.2.5");

         _manifest
             .DefineStyle("bootstrap-calendar")
             .SetUrl($"~/{Common.ModuleName}/Styles/calendar.min.css", $"~/{Common.ModuleName}/Styles/calendar.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-calendar/0.2.5/css/calendar.min.css", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-calendar/0.2.5/css/calendar.css")
             .SetCdnIntegrity("sha512-zmbBaOaW0O7Zo4gYPd/Ef9sAMiv1doq5erbIWhuYcfGacLP4hscq/V45c5CwYKhqO9ffFW+YYCyhqY+WJJyO4g==", "sha512-z3HKbBdaba6ixpItVs/TjdnOEQvJO6Zf3pK4T7zzncPYNz10J4y/4CeJ+2iGmut86H7PMAK74duJP9KLKCT3UA==")
             .SetVersion("0.2.5");

         _manifest
            .DefineScript("underscore")
            .SetUrl($"~/{Common.ModuleName}/Scripts/underscore.min.js", $"~/{Common.ModuleName}/Scripts/underscore.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/underscore.js/1.4.1/underscore-min.js", "https://cdnjs.cloudflare.com/ajax/libs/underscore.js/1.4.1/underscore.js")
            .SetCdnIntegrity("sha512-xHPfYya0Ac9NYcp0d6YKVnP/n7dcRGiQCsGKC+BMpziXwgg/6VogplMOS+nqUXQIPmtuGwZ25fAcSgtjBxBVfg==", "sha512-qbJl2/KyecDVJdgAwr0Gzhm5KBmkzdUouG+sKmCWP1Idb144ZnfSmCRReuYXPEHIa4j38TwrR2ySeGoFH5gKzA==")
            .SetVersion("1.4.1");

         /* scripts for jquery file upload (blueimp) */

         _manifest
            .DefineScript("load-image-all")
            .SetUrl($"~/{Common.ModuleName}/Scripts/load-image.all.min.js", $"~/{Common.ModuleName}/Scripts/load-image.all.min.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/blueimp-load-image/5.14.0/load-image.all.min.js", "https://cdnjs.cloudflare.com/ajax/libs/blueimp-load-image/5.14.0/load-image.all.min.js")
            .SetCdnIntegrity("sha512-HZg0q8NV+VTxnU6hdkK0rL+fSmTGCbXZ3mHjqCCi87St5QRdvXENfRxkMK692inskRsCPee07d7VwcKNWaByCQ==", "sha512-HZg0q8NV+VTxnU6hdkK0rL+fSmTGCbXZ3mHjqCCi87St5QRdvXENfRxkMK692inskRsCPee07d7VwcKNWaByCQ==")
            .SetVersion("5.14.0");

         _manifest
            .DefineScript("canvas-to-blob")
            .SetUrl($"~/{Common.ModuleName}/Scripts/canvas-to-blob.js", $"~/{Common.ModuleName}/Scripts/canvas-to-blob.min.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/javascript-canvas-to-blob/3.28.0/js/canvas-to-blob.min.js", "https://cdnjs.cloudflare.com/ajax/libs/javascript-canvas-to-blob/3.28.0/js/canvas-to-blob.js")
            .SetCdnIntegrity("sha512-Q0RYciuNUJLzUgA9T0A0yj1A5wR+rM7MDr3hKBJuNBaSW5O2TqRyBMMLswe7Z/M9czXRnD/5mh1/xQpDpxFZaA==", "sha512-QinObFNs7mVBtipyw8BEERLHKQ1P2n5Wbxd8Kt+G9ST/lp99qZKlJUUsNSZYSPf/yGL7eNN0UCcDaMiZjXMtGg==")
            .SetVersion("3.28.0");

         _manifest
            .DefineScript("jquery-fileupload")
            .SetDependencies("jQuery-ui")
            .SetUrl($"~/{Common.ModuleName}/Scripts/jquery.fileupload.js", $"~/{Common.ModuleName}/Scripts/jquery.fileupload.min.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload.min.js", "https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload.js")
            .SetCdnIntegrity("sha512-qPkNWpUqYz8bhO5bGNPBvlCB9hPZBil2ez5Mo8yVmpCKI315UDDPQeg/TE7KwZ+U/wdSO8JguwVxYY/Ha7U+vQ==", "sha512-mPBKSUQd9V0RWuHe31Q2nvLSUSBOh4yjW21MBFIYdR8PPZL1mDU9clClGv9SwVSJC9m44+wcfljUPSqWTv91Xg==")
            .SetVersion("10.31.0");

         _manifest
            .DefineScript("jquery-fileupload-process")
            .SetDependencies("jquery-fileupload:10.31.0")
            .SetUrl($"~/{Common.ModuleName}/Scripts/jquery.fileupload-process.js", $"~/{Common.ModuleName}/Scripts/jquery.fileupload-process.min.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload-process.min.js", "https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload-process.js")
            .SetCdnIntegrity("sha512-/PpB/cqFe0WgMQRLimSpDJtdyl3sRyo0bxwtiapDaYhs3AkEeNnDUBKtdLKpEEE2X/Xr2YzfCZTdLIOSC2JHVA==", "sha512-nSNTn7MSa1e/I0wf7fi4vbSAQVgO1x3Tum/spzI00lOV+Yd9uJWH18jjYiwx4WHVj3p7UR6tF2de7U094x5rCw==")
            .SetVersion("10.31.0");

         _manifest
            .DefineScript("jquery-fileupload-image")
            .SetDependencies("jquery-fileupload-process:10.31.0", "load-image-all:5.14.0", "canvas-to-blob:3.28.0")
            .SetUrl($"~/{Common.ModuleName}/Scripts/jquery.fileupload-image.js", $"~/{Common.ModuleName}/Scripts/jquery.fileupload-image.min.js")
            .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload-image.min.js", "https://cdnjs.cloudflare.com/ajax/libs/blueimp-file-upload/10.31.0/js/jquery.fileupload-image.js")
            .SetCdnIntegrity("sha512-/zXq+I7ihnFX2Jw9+7lNZX9/oZ323b3rOMtwtowHN2VS3xoeLY1srC11oiQidw1YDTxrHVLWp9dehlZqqLKqhg==", "sha512-e0MZer7eLu4GULyhtYxafD0hyDK9anMSL27EqC4H/c1paOzw0xT4d7nuat2KG+Np65HQzz9fmO1HeW1bt6P9MQ==")
            .SetVersion("10.31.0");

         /* Chart.js */
         _manifest
            .DefineScript("chart-js")
            .SetUrl($"~/{Common.ModuleName}/Scripts/chart.umd.js", $"~/{Common.ModuleName}/Scripts/chart.umd.min.js")
            .SetCdn("https://cdn.jsdelivr.net/npm/chart.js@4.5.1/dist/chart.umd.js", "https://cdn.jsdelivr.net/npm/chart.js@4.5.1/dist/chart.umd.min.js")
            .SetCdnIntegrity("sha384-hfkuqrKeWFmnTMWN31VWyoe8xgdTADD11kgxmdpx2uyE6j5Az5uZq6u6AKYYmAOw", "sha384-jb8JQMbMoBUzgWatfe6COACi2ljcDdZQ2OxczGA3bGNeWe+6DChMTBJemed7ZnvJ")
            .SetVersion("4.5.1");

         _manifest
            .DefineScript("charts-loader")
            .SetUrl($"~/{Common.ModuleName}/Scripts/chart/charts-loader.js")
            .SetVersion("1.0.3")
            .SetDependencies("chart-js");

         _manifest
            .DefineStyle("dashboard-charts")
            .SetUrl($"~/{Common.ModuleName}/Styles/dashboard/dashboard-charts.css")
            .SetVersion("1.0.0")
            .SetDependencies("chart-js");

      }

      public void Configure(ResourceManagementOptions options) {
         options.ResourceManifests.Add(_manifest);
      }

   }
}
