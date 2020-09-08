using OrchardCore.ContentManagement;
using OrchardCore.ContentFields.Fields;

namespace TransformalizeModule.Models {
   public class TransformalizeFilePart : ContentPart {
      public TransformalizeFilePart() {
         OriginalName = new TextField { Text = string.Empty };
         FullPath = new TextField { Text = string.Empty };
      }

      public TextField OriginalName { get; set; }
      public TextField FullPath { get; set; }
   }
}
