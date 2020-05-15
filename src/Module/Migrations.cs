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

         return 1;
      }

      public int UpdateFrom1() {
         _contentDefinitionManager.AlterPartDefinition("TransformalizeReportPart", part => part
             .WithField("PageSizes", field => field
                 .OfType(nameof(PageSizesField))
                 .WithDisplayName("Page Sizes")
             )
         );

         return 2;
      }

      public int UpdateFrom2() {

         _contentDefinitionManager.AlterPartDefinition("TransformalizeTaskPart", part => part
             .WithDisplayName("Transformalize Task Part")
             .WithDescription("Provides fields for Transformalize Task content type")
             .WithField("Arrangement", field => field
                 .OfType(nameof(TransformalizeArrangementField))
                 .WithDisplayName("Arrangement")
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
         return 3;
      }

   }
}
