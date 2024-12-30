using Microsoft.Extensions.Logging;
using OrchardCore.Alias.Settings;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Features.Services;

namespace ProxyModule {
   public class Migrations : DataMigration {

      private readonly IContentDefinitionManager _contentDefinitionManager;
      private readonly IModuleService _moduleService;
      private readonly ILogger<Migrations> _logger;

      public Migrations(
         IContentDefinitionManager contentDefinitionManager,
         IModuleService moduleService,
         ILogger<Migrations> logger
      ) {
         _contentDefinitionManager = contentDefinitionManager;
         _moduleService = moduleService;
         _logger = logger;
      }

      public int Create() {

         _contentDefinitionManager.AlterPartDefinitionAsync("ProxyPart", part => part
             .WithDisplayName("Proxy Part")
             .WithDescription("Fields for Proxy content type")
             .WithField("ServiceUrl", field => field
                 .OfType(nameof(TextField))
                 .WithDisplayName("Service URL")
                 .WithPosition("3")
                 .WithSettings(new TextFieldSettings {
                    Hint = string.Empty,
                    Required = true
                 }
                 )
             ).WithField("ForwardHeaders", field => field
             .OfType(nameof(BooleanField))
             .WithDisplayName("Forward Headers")
             .WithPosition("4")
             .WithSettings(new BooleanFieldSettings {
                DefaultValue = false,
                Hint = "forward request headers along with request to private resource",
                Label = "Forward Headers"
             }
             )
           )
         );

         _contentDefinitionManager.AlterTypeDefinitionAsync("Proxy", builder => builder
             .Creatable()
             .Listable()
             .WithPart("TitlePart", part => part.WithPosition("1"))
             .WithPart("AliasPart", part => part
                 .WithPosition("2")
                 .WithSettings(new AliasPartSettings {
                    Pattern = "{{ ContentItem | title | slugify }}"
                 })
             )
             .WithPart("ProxyPart", part => part.WithPosition("3"))
             .WithPart("CommonPart", part => part.WithPosition("4"))
         );

         return 1;
      }

      private async Task EnableFeature(string id) {

         var availableFeatures = await _moduleService.GetAvailableFeaturesAsync();

         var contentFields = availableFeatures.FirstOrDefault(f => f.Descriptor.Id == id);
         if (contentFields != null) {
            if (!contentFields.IsEnabled) {
               _logger.LogInformation($"Enabling {id}");
               await _moduleService.EnableFeaturesAsync(new[] { id });
            }
         } else {
            _logger.LogError($"Unable to find {id} features required for Proxy.");
         }
      }
   }
}
