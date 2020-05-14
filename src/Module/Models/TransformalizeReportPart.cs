using OrchardCore.ContentManagement;
using Module.Fields;

namespace Module.Models {
   public class TransformalizeReportPart : ContentPart {
      public TransformalizeArrangementField Arrangement { get; set; }
      public PageSizesField PageSizes { get; set; }
   }
}
