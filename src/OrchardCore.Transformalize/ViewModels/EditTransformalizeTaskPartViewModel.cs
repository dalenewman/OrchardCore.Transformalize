using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeTaskPartViewModel {

      [BindNever]
      public TransformalizeTaskPart TransformalizeTaskPart { get; set; }
      public TextField Arrangement { get; set; }
   }
}