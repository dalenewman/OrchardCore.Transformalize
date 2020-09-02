using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace TransformalizeModule.Models {
   public class TransformalizeFormPart : ContentPart {

      public TransformalizeFormPart() {
         Arrangement = new TextField();
      }
      public TextField Arrangement { get; set; }
   }
}
