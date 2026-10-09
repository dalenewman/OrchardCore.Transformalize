using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormUploadViewModel {
      public required Parameter Parameter { get; init; }
      public bool Valid { get; init; }
      public bool Scan => Parameter.InputType == "scan";
   }
}
