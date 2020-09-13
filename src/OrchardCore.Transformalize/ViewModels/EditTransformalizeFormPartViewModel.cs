using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeFormPartViewModel {
      public TransformalizeFormPart TransformalizeFormPart { get; set; }
      public TextField Arrangement { get; set; }
      public string CreateCommand { get; set; }
      public string InsertCommand { get; set; }
      public string UpdateCommand { get; set; }
   }
}