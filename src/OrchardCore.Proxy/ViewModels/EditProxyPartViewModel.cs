using OrchardCore.ContentFields.Fields;
using ProxyModule.Models;

namespace ProxyModule.ViewModels {
   public class EditProxyPartViewModel {
      public ProxyPart ProxyPart { get; set; }
      public TextField ServiceUrl { get; set; }
      public BooleanField ForwardHeaders { get; set; }
   }
}