using OrchardCore.ContentManagement;
using TransformalizeModule.Fields;

namespace TransformalizeModule.Models {
   public class TransformalizeTaskPart : ContentPart {

      public TransformalizeTaskPart() {
         Arrangement = new TransformalizeArrangementField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
   }
}
