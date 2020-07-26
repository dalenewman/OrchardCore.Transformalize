using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeReportPartViewModel {
      public TransformalizeReportPart TransformalizeReportPart { get; set; }
      public TextField Arrangement { get; set; }
      public TextField PageSizes { get; set; }
      public BooleanField BulkActions { get; set; }
      public TextField BulkActionValueField { get; set; }
   }
}