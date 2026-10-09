using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormDateViewModel {
      public required Parameter Parameter { get; init; }
      public bool Valid { get; init; }
      public bool Interactive { get; init; }
   }
}
