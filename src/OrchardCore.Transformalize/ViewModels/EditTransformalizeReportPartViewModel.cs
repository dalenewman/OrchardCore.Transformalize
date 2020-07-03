using OrchardCore.TransformalizeModule.Models;

namespace OrchardCore.TransformalizeModule.ViewModels {
   public class EditTransformalizeReportPartViewModel {
      public TransformalizeReportPart TransformalizeReportPart { get; set; }
      public string Arrangement { get; set; }
      public string PageSizes { get; set; }
      public bool BulkActions { get; set; }
      public string BulkActionValueField { get; set; }
   }
}