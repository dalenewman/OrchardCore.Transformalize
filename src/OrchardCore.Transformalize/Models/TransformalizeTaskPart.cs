using OrchardCore.ContentManagement;
using OrchardCore.TransformalizeModule.Fields;

namespace OrchardCore.TransformalizeModule.Models {
   public class TransformalizeTaskPart : ContentPart {

      public TransformalizeTaskPart() {
         Arrangement = new TransformalizeArrangementField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
   }
}
