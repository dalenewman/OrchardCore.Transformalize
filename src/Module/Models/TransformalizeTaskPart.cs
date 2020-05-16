using OrchardCore.ContentManagement;
using Module.Fields;

namespace Module.Models {
   public class TransformalizeTaskPart : ContentPart {

      public TransformalizeTaskPart() {
         Arrangement = new TransformalizeArrangementField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
   }
}
