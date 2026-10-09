using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormMapViewModel {
      public required Process Process { get; init; }
      public required Parameter Parameter { get; init; }
      public bool Interactive { get; init; }
      public bool Valid { get; init; }
      public string LongitudeName => Process.Parameters.FirstOrDefault(p => p.InputType == "map" && p.InputCapture == "longitude")?.Name ?? string.Empty;
   }
}
