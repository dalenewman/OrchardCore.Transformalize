using OrchardCore.ResourceManagement;

namespace Module {
   public class ResourceManifest : IResourceManifestProvider {
      public void BuildManifests(IResourceManifestBuilder builder) {
         var manifest = builder.Add();

         /* CodeMirror */
         manifest
          .DefineScript("codemirror")
          .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/codemirror.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/codemirror.js")
          .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/codemirror.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/codemirror.js")
          .SetCdnIntegrity("sha256-id5Qk/MwQJxgNlDFDpVymUuReXfTUZiaQKb8arrddQM=", "sha256-C1vNlVkHhwXPBxyzyyUmeHeOdlVRzbzC+teAGXhUEUc=")
          .SetVersion("5.52.2");

         manifest
             .DefineStyle("codemirror")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/codemirror.min.css", $"~/{Common.ModuleName}/Scripts/codemirror/codemirror.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/codemirror.min.css", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/codemirror.css")
             .SetCdnIntegrity("sha256-vZ3SaLOjnKO/gGvcUWegySoDU6ff33CS5i9ot8J9Czk=", "sha256-PWWDlCsO+b6rKHz9dgBeS5cqTmBbebfAtNXnEqy3Xvc=")
             .SetVersion("5.52.2");


         /* CodeMirror XML */
         manifest
             .DefineScript("codemirror-mode-xml")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/mode/xml/xml.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/mode/xml/xml.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/mode/xml/xml.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/mode/xml/xml.js")
             .SetCdnIntegrity("sha256-Lfk8z6WUsBN6YiCaMpH6bxBHyRqkPK4O2QbQHFNUS40=", "sha256-yhHPVEbMcHCb0TOtv6Leq8f3VEVe3+Ot0oCy83K+jvs=")
             .SetVersion("5.52.2");

         /* Full Screen */
         manifest
                .DefineScript("codemirror-addon-display-fullscreen")
                .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/display/fullscreen.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/display/fullscreen.js")
                .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/display/fullscreen.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/display/fullscreen.js")
                .SetCdnIntegrity("sha256-ttglgk8dprl46qouhLrnP75y3ykP97gJf53RKg9htE4=", "sha256-HhKG06Ib+xZ6RYvmBap8s0os+CEXWPArOCet4VaME6Q=")
                .SetVersion("5.52.2");

         /* Fold Gutter */
         manifest
             .DefineScript("codemirror-addon-fold-foldgutter")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldgutter.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldgutter.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldgutter.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldgutter.js")
             .SetCdnIntegrity("sha256-FAVRbCrAsSe4mCyvHf9LZ8u7nT0d7gwaC1MhJzxoTNE=", "sha256-UjqbKBTcJwani3fXnb3B6UWguF8iEYAouMsTn425SKU=")
             .SetVersion("5.52.2");

         /* Dialog */
         manifest
             .DefineScript("codemirror-addon-dialog-dialog")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/dialog/dialog.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/dialog/dialog.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/dialog/dialog.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/dialog/dialog.js")
             .SetCdnIntegrity("sha256-G+QhvxjUNi5P5cyQqjROwriSUy2lZtCFUQh+8W1o6I0=", "sha256-HuoOUA3OENhZTY1oGpHRtxpCCTVcnCmIXobiay12aF4=")
             .SetVersion("5.52.2");

         /* Show Hint */
         manifest
             .DefineScript("codemirror-addon-hint-show-hint")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/show-hint.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/show-hint.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/show-hint.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/show-hint.js")
             .SetCdnIntegrity("sha256-s030a2NmcCVudLqFb85WQPTfkO1LlDXHxo3XuWxGYGA=", "sha256-c5yCM5a9nH56AOWRp59e1TjsRi1EPfsAHf0O7WFl30s=")
             .SetVersion("5.52.2");

         /* Any Word Hint */
         manifest
             .DefineScript("codemirror-addon-hint-anyword-hint")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/anyword-hint.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/anyword-hint.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/anyword-hint.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/anyword-hint.min.js")
             .SetCdnIntegrity("sha256-RAnsThvT8u3giTsRpAwZ+KWs/LPrEdNtwq2GyPb/RsQ=", "sha256-lCVMIcSF33YZe03bIZs2h/Cs2TIWgZGvmIs8FTYO3e0=")
             .SetVersion("5.52.2");

         /* Fold Code */
         manifest
             .DefineScript("codemirror-addon-fold-foldcode")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldcode.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldcode.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldcode.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldcode.js")
             .SetCdnIntegrity("sha256-qhqsqHF0ep05BTkxty6v8+3968ipN9bdCxXx5Q5dbgk=", "sha256-eHCxXsZStfhMVmmDh1UQZwVz6RDWmxDTohRrwg/Wifo=")
             .SetVersion("5.52.2");

         /* Search */
         manifest
             .DefineScript("codemirror-addon-search-search")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/search/search.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/search/search.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/search.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/search.js")
             .SetCdnIntegrity("sha256-pk1ahN30IsCG20LJu38Va1A7tQagksJwAJUJK3rBFe0=", "sha256-iUnNlgkrU5Jj8oKl2zBBCTmESI2xpXwZrTX+arxSEKc=")
             .SetVersion("5.52.2");

         /* Search Cursor */
         manifest
             .DefineScript("codemirror-addon-search-searchcursor")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/search/searchcursor.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/search/searchcursor.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/searchcursor.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/searchcursor.js")
             .SetCdnIntegrity("sha256-LE6iKRf3wBxVYAW8kInWVcU3A9hYtDaphl4Wgdg4ZkU=", "sha256-B1aSPEmOtZ0K3toDpFwq4H3V3nbtCpIziOHNOG80wkk=")
             .SetVersion("5.52.2");

         /* Search Jump to Line */
         manifest
             .DefineScript("codemirror-addon-search-jump-to-line")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/search/jump-to-line.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/search/jump-to-line.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/jump-to-line.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/jump-to-line.js")
             .SetCdnIntegrity("sha256-6hE+UvbWF7EVpwVlstz+DltSX0qu32C/v5neucv+f0E=", "sha256-sMswWLNouxW7Fh3iOm5ozacSAnrp9shgUaJomWMlDtQ=")
             .SetVersion("5.52.2");

         /* Search Match Highlighter */
         manifest
             .DefineScript("codemirror-addon-search-match-highlighter")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/search/match-highlighter.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/search/match-highlighter.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/match-highlighter.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/search/match-highlighter.js")
             .SetCdnIntegrity("sha256-1ldu9k2AN9gh9HWCK61lA5l/XVCZ7qsExtiEPl9J19w=", "sha256-PtGq+Pjt1eqhreHfFTNKzfdrX59WufE+PQRJIJop9XM=")
             .SetVersion("5.52.2");

         /* XML Fold */
         manifest
             .DefineScript("codemirror-addon-fold-xml-fold")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/xml-fold.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/xml-fold.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/xml-fold.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/xml-fold.js")
             .SetCdnIntegrity("sha256-6CqMc7XMXXv0ffuJv52X0GHqK1/q7PjETdy2GOLIhAY=", "sha256-6qrza98BMjZqcPGsh7xpweWNL52WV7KmoCz6NUkA1qo=")
             .SetVersion("5.52.2");

         /* Close Tag */
         manifest
             .DefineScript("codemirror-addon-edit-closetag")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/edit/closetag.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/edit/closetag.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/edit/closetag.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/edit/closetag.js")
             .SetCdnIntegrity("sha256-HNFx5VDaZS4Zl4JNDio1E6ISPgCbPmP/LU/wug8PS08=", "sha256-cAuKp8RuBAKH4y/9HtuGpC0J7cWyqVlCQVkLFUwCMr0=")
             .SetVersion("5.52.2");

         /* Match Tags */
         manifest
             .DefineScript("codemirror-addon-edit-matchtags")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/edit/matchtags.min.js", $"~/{Common.ModuleName}/Scripts/codemirror/addon/edit/matchtags.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/edit/matchtags.min.js", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/edit/matchtags.js")
             .SetCdnIntegrity("sha256-7+ar9rS4zfA49+LlLzDc0O7Wzf7tFqxTjo38KHBObAA=", "sha256-oCAwj6P1/BzATEuHMQxLOWONXkQHh4FLz8JFcIH/+hQ=")
             .SetVersion("5.52.2");

         /* Full Screen Style */
         manifest
             .DefineStyle("codemirror-addon-display-fullscreen")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/display/fullscreen.css", $"~/{Common.ModuleName}/Scripts/codemirror/addon/display/fullscreen.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/display/fullscreen.css", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/display/fullscreen.css")
             .SetCdnIntegrity("sha256-SpuaNYgDjBMdeyjrjtsC+U5fpSDpftPNv7oO8HQvG7w=", "sha256-SpuaNYgDjBMdeyjrjtsC+U5fpSDpftPNv7oO8HQvG7w=")
             .SetVersion("5.52.2");

         /* Fold Gutter Style */
         manifest
             .DefineStyle("codemirror-addon-fold-foldgutter")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldgutter.min.css", $"~/{Common.ModuleName}/Scripts/codemirror/addon/fold/foldgutter.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldgutter.min.css", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/fold/foldgutter.css")
             .SetCdnIntegrity("sha256-PAZt4Yo+uLbowOVolpiWbNrg1VUtA43Zvw/TPBABeaQ=", "sha256-V27800C3cLYNd9jCnp3za/WFdjkb6rUbQ5EU5O0BrFY=")
             .SetVersion("5.52.2");

         /* Dialog Style */
         manifest
             .DefineStyle("codemirror-addon-dialog-dialog")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/dialog/dialog.min.css", $"~/{Common.ModuleName}/Scripts/codemirror/addon/dialog/dialog.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/dialog/dialog.min.css", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/dialog/dialog.min.css")
             .SetCdnIntegrity("sha256-OjF42ew3ra0/zNWgjfDTydf609RXv0cXcassXEeW0O8=", "sha256-XfaQ13HxIRg0hWLdKpAGBDOuLt7M0JCKvKpEgLHj5Gg=")
             .SetVersion("5.52.2");

         /* Show Hint Style */
         manifest
             .DefineStyle("codemirror-addon-hint-show-hint")
             .SetUrl($"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/show-hint.min.css", $"~/{Common.ModuleName}/Scripts/codemirror/addon/hint/show-hint.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/show-hint.min.css", "https://cdnjs.cloudflare.com/ajax/libs/codemirror/5.52.2/addon/hint/show-hint.css")
             .SetCdnIntegrity("sha256-Ng5EdzHS/CC37tR7tE75e4Th9+fBvOB4eYITOkXS22Q=", "sha256-9PjI1IwVXcv44E37KbDVAJdEYNCCBK3+Ddo757s6L8g=")
             .SetVersion("5.52.2");

         /* bootstrap select */
         //manifest
         //    .DefineScript("bootstrap-select")
         //    .SetDependencies("jQuery")
         //    .SetUrl($"~/{Common.ModuleName}/Scripts/bootstrap-select.min.js", $"~/{Common.ModuleName}/Scripts/bootstrap-select.js")
         //    .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.14/js/bootstrap-select.min.js", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.14/js/bootstrap-select.js")
         //    .SetCdnIntegrity("sha256-Z2PecxUNJuMpbNAVT/Et7eO105JRxzTArk+K9OQ35Mw=", "sha256-vliZ49knxz5XADj1rEcFhHdlGcnR7KVX8uzgGJMM44Q=")
         //    .SetVersion("1.13.14");

         //manifest
         //    .DefineStyle("bootstrap-select")
         //    .SetUrl($"~/{Common.ModuleName}/Styles/bootstrap-select.min.css", $"~/{Common.ModuleName}/Styles/bootstrap-select.css")
         //    .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.14/css/bootstrap-select.min.css", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.14/css/bootstrap-select.css")
         //    .SetCdnIntegrity("sha256-aSeb7knF81Avej8HNY2hVizAoQ1X6KVs/A+Jd9NnyZ8=", "sha256-AyxV+AkmiL2W4kKrXBt8Y9jAS9UC70v62yd+esV930c=")
         //    .SetVersion("1.13.14");

         manifest
             .DefineScript("bootstrap-select")
             .SetDependencies("jQuery")
             .SetUrl($"~/{Common.ModuleName}/Scripts/bootstrap-select.min.js", $"~/{Common.ModuleName}/Scripts/bootstrap-select.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.12/js/bootstrap-select.min.js", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.12/js/bootstrap-select.js")
             .SetCdnIntegrity("sha256-+o/X+QCcfTkES5MroTdNL5zrLNGb3i4dYdWPWuq6whY=", "sha256-dTbSmcD6nzs7MFxtiBSI9WD1AWlUURe4sbYAWRsIkqI=")
             .SetVersion("1.13.12");

         manifest
             .DefineStyle("bootstrap-select")
             .SetUrl($"~/{Common.ModuleName}/Styles/bootstrap-select.min.css", $"~/{Common.ModuleName}/Styles/bootstrap-select.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.12/css/bootstrap-select.min.css", "https://cdnjs.cloudflare.com/ajax/libs/bootstrap-select/1.13.12/css/bootstrap-select.css")
             .SetCdnIntegrity("sha256-l3FykDBm9+58ZcJJtzcFvWjBZNJO40HmvebhpHXEhC0=", "sha256-+oD8XVy0yTlpJi12d9AW34KibDEViR8XPho0d127HZA=")
             .SetVersion("1.13.12");

         // Prismjs
         manifest
             .DefineScript("prism")
             .SetUrl($"~/{Common.ModuleName}/Scripts/prismjs/prism.min.js", $"~/{Common.ModuleName}/Scripts/prismjs/prism.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/prism.min.js", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/prism.js")
             .SetCdnIntegrity("sha256-3teItwIfMuVB74Alnxw/y5HAZ2irOsCULFff3EgbtEs=", "sha256-ooBCsFqD8/ih41/S1sKjWdtTyGWUuetgVvUWRF1CbN0=")
             .SetVersion("1.20.0");

         manifest
             .DefineScript("prism-markup")
             .SetUrl($"~/{Common.ModuleName}/Scripts/prismjs/components/prism-markup.min.js", $"~/{Common.ModuleName}/Scripts/prismjs/components/prism-markup.js")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/components/prism-markup.min.js", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/components/prism-markup.js")
             .SetCdnIntegrity("sha256-Jp/hNSrSnxiyz2YWkA6uZSI7FMULRHemA0yA/5BhEdU=", "sha256-n9xE+cin+LSEDenLFLMs1i+Q/EidigipCXNw/Hx+4Ys=")
             .SetVersion("1.20.0");

         manifest
             .DefineStyle("prism")
             .SetUrl($"~/{Common.ModuleName}/Styles/prismjs/prism.min.css", $"~/{Common.ModuleName}/Styles/prismjs/prism.min.css")
             .SetCdn("https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/themes/prism.min.css", "https://cdnjs.cloudflare.com/ajax/libs/prism/1.20.0/themes/prism.css")
             .SetCdnIntegrity("sha256-cuvic28gVvjQIo3Q4hnRpQSNB0aMw3C+kjkR0i+hrWg=", "sha256-LArq5Cddn4QETLgVbBZppSkBLJbL1dhacPVQF/EPH00=")
             .SetVersion("1.20.0");

      }
   }
}
