using OrchardCore.ContentManagement;
using Module.Fields;

namespace Module.Models {
   public class TransformalizeReportPart : ContentPart {
      public TransformalizeReportPart() {
         Arrangement = new TransformalizeArrangementField();
         PageSizes = new PageSizesField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
      public PageSizesField PageSizes { get; set; }
   }
}
