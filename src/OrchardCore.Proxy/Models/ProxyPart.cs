using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace ProxyModule.Models {
   public class ProxyPart : ContentPart {
      public ProxyPart() {

         ServiceUrl = new TextField();
         ForwardHeaders = new BooleanField();
      }
      public TextField ServiceUrl { get; set; }
      public BooleanField ForwardHeaders { get; set; }
   }
}
