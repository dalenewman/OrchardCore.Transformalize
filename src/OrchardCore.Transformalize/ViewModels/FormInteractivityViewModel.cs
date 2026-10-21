using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormInteractivityViewModel {
      public required Process Process { get; init; }
      public required string ValidationUrl { get; init; }
      public required string SubmitUrl { get; init; }
      public string? RunUrl { get; init; }
      public bool LocationEnableHighAccuracy { get; init; }
      public int LocationMaximumAge { get; init; }
      public int LocationTimeout { get; init; } = -1;
      public bool HasLocation => Process.Parameters.Any(p => p.InputType == "location");
      public bool HasUploads => Process.Parameters.Any(p => p.InputType is "file" or "scan");
      public bool HasPlaces => Process.Parameters.Any(p => p.InputType == "google-places-autocomplete");
      public bool HasMap => Process.Parameters.Any(p => p.InputType == "map");

      public string ActionUrl => RunUrl != null && Process.Parameters.All(p => p.Valid) ? RunUrl : SubmitUrl;

   }
}
