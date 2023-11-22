using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ContentFields.Fields;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class EditTransformalizeFilePartViewModel {

      [BindNever]
      public TransformalizeFilePart TransformalizeFilePart { get; set; }
      public TextField OriginalName { get; set; }
      public TextField FullPath { get; set; }
   }
}