using Module.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace Module.ViewModels {
   public class DisplayTransformalizeArrangementFieldViewModel {
      public string Arrangement => Field.Arrangement;
      public TransformalizeArrangementField Field { get; set; }
      public ContentPart Part { get; set; }
      public ContentPartFieldDefinition PartFieldDefinition { get; set; }
   }
}
