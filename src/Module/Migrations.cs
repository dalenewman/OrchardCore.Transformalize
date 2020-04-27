using OrchardCore.Alias.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using Module.Fields;

namespace Module {
   public class Migrations : DataMigration {
      private readonly IContentDefinitionManager _contentDefinitionManager;

      public Migrations(IContentDefinitionManager contentDefinitionManager) {
         _contentDefinitionManager = contentDefinitionManager;
      }

      public int Create() {

         _contentDefinitionManager.AlterPartDefinition("TransformalizeReportPart", part => part
             .WithDisplayName("Transformalize Report Part")
             .WithDescription("Provides fields for Transformalize Report content type")
             .WithField("Arrangement", field => field
                 .OfType(nameof(TransformalizeArrangementField))
                 .WithDisplayName("Arrangement")
             )
         );

         _contentDefinitionManager.AlterTypeDefinition("TransformalizeReport", builder => builder
             .Creatable()
             .Listable()
             .WithPart("TitlePart", part => part.WithPosition("01"))
             .WithPart("AliasPart", part => part
                 .WithPosition("02")
                 .WithSettings(new AliasPartSettings {
                    Pattern = "{{ ContentItem | title | slugify }}"
                 })
             )
             .WithPart("TransformalizeReportPart", part => part.WithPosition("03"))
             .WithPart("CommonPart", part => part.WithPosition("04"))
         );

         return 1;
      }
   }
}
