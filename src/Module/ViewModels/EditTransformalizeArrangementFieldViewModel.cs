using Module.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace Module.ViewModels {
   public class EditTransformalizeArrangementFieldViewModel {
      public string Arrangement { get; set; }
      public TransformalizeArrangementField Field { get; set; }
      public ContentPart Part { get; set; }
      public ContentPartFieldDefinition PartFieldDefinition { get; set; }
   }
}
