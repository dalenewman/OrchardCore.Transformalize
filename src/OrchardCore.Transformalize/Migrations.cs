using OrchardCore.Alias.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Features.Services;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace TransformalizeModule {
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

         _contentDefinitionManager.AlterPartDefinition("TransformalizeReportPart", part => part
             .WithDisplayName("Transformalize Report Part")
             .WithDescription("Fields for Transformalize Report content type")
             .WithField("Arrangement", field => field
                 .OfType(nameof(TextField))
                 .WithDisplayName("Arrangement")
                 .WithPosition("01")
                 .WithEditor("TransformalizeArrangement")
                 .WithSettings(new TextFieldSettings {
                    Hint = string.Empty,
                    Required = true
                 }
                 )
             ).WithField("PageSizes", field => field
                 .OfType(nameof(TextField))
                 .WithDisplayName("Page Sizes")
                 .WithPosition("02")
                 .WithSettings(new TextFieldSettings {
                    Required = false,
                    Hint = "To overide default page sizes, specify a comma delimited list of page sizes (integers). To use the common page sizes defined in settings, leave it blank.  To disable pagination altogether, set this to 0."
                 }
                )
             ).WithField("BulkActions", field => field
                .OfType(nameof(BooleanField))
                .WithDisplayName("Bulk Actions")
                .WithPosition("03")
                .WithSettings(new BooleanFieldSettings {
                   DefaultValue = false,
                   Hint = "Allow user to select one, many, or all records for a bulk action?",
                   Label = "Bulk Actions"
                }
                )
              ).WithField("BulkActionValueField", field => field
                .OfType(nameof(TextField))
                .WithDisplayName("Bulk Action Value Field")
                .WithPosition("04")
                .WithSettings(new TextFieldSettings {
                   Required = false,
                   Hint = "Specify which field or calculated field provides the value for bulk actions."
                }
                )
              ).WithField("BulkActionCreateTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Create Task")
                  .WithPosition("05")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionCreateTaskHint
                  })
              ).WithField("BulkActionWriteTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Write Task")
                  .WithPosition("06")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionWriteTaskHint
                  })
              ).WithField("BulkActionSummaryTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Summary Task")
                  .WithPosition("07")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionSummaryTaskHint
                  })
              ).WithField("BulkActionRunTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Run Task")
                  .WithPosition("08")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionRunTaskHint
                  })
              ).WithField("BulkActionSuccessTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Success Task")
                  .WithPosition("09")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionSuccessTaskHint
                  })
              ).WithField("BulkActionFailTask", field => field
                  .OfType(nameof(TextField))
                  .WithDisplayName("Bulk Action Fail Task")
                  .WithPosition("10")
                  .WithSettings(new TextFieldSettings {
                     Required = false,
                     Hint = Common.BulkActionFailTaskHint
                  })
              )
         );

         _contentDefinitionManager.AlterTypeDefinition("TransformalizeReport", builder => builder
             .Creatable()
             .Listable()
             .WithPart("TitlePart", part => part.WithPosition("1"))
             .WithPart("AliasPart", part => part
                 .WithPosition("2")
                 .WithSettings(new AliasPartSettings {
                    Pattern = "{{ ContentItem | title | slugify }}"
                 })
             )
             .WithPart("TransformalizeReportPart", part => part.WithPosition("3"))
             .WithPart("CommonPart", part => part.WithPosition("4"))
         );

         _contentDefinitionManager.AlterPartDefinition("TransformalizeTaskPart", part => part
             .WithDisplayName("Transformalize Task Part")
             .WithDescription("Provides fields for Transformalize Task content type")
             .WithField("Arrangement", field => field
                 .OfType(nameof(TextField))
                 .WithDisplayName("Arrangement")
                 .WithPosition("1")
                 .WithEditor("TransformalizeArrangement")
                 .WithSettings(new TextFieldSettings {
                    Hint = string.Empty,
                    Required = true
                 }
               )
            )
         );

         _contentDefinitionManager.AlterTypeDefinition("TransformalizeTask", builder => builder
             .Creatable()
             .Listable()
             .WithPart("TitlePart", part => part.WithPosition("1"))
             .WithPart("AliasPart", part => part
                 .WithPosition("2")
                 .WithSettings(new AliasPartSettings {
                    Pattern = "{{ ContentItem | title | slugify }}"
                 })
             )
             .WithPart("TransformalizeTaskPart", part => part.WithPosition("3"))
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
            _logger.LogError($"Unable to find {id} features required for Transformalize.");
         }
      }
   }
}
