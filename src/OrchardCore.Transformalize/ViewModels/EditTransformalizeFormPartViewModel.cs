using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeFormPartViewModel {

      [BindNever]
      public TransformalizeFormPart TransformalizeFormPart { get; set; }
      public TextField Arrangement { get; set; }
      public BooleanField LocationEnableHighAccuracy { get; set; }
      public NumericField LocationMaximumAge { get; set; }
      public NumericField LocationTimeout { get; set; }
      public string CreateCommand { get; set; } = string.Empty;
      public string InsertCommand { get; set; } = string.Empty;
      public string UpdateCommand { get; set; } = string.Empty;
      public string SelectCommand { get; set; } = string.Empty;
   }
}