using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeReportPartViewModel {
      public TransformalizeReportPart TransformalizeReportPart { get; set; }
      public TransformalizeArrangementField Arrangement { get; set; }
      public PageSizesField PageSizes { get; set; }
      public BooleanField BulkActions { get; set; }
      public TextField BulkActionValueField { get; set; }
   }
}