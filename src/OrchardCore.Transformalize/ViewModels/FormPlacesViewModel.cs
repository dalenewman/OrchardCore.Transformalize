using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormPlacesViewModel {
      public required Process Process { get; init; }
      public required Parameter Parameter { get; init; }
      public bool Valid { get; init; }

      public Dictionary<string, string> ComponentTargets {
         get {
            var targets = new Dictionary<string, string>();
            var map = Process.Maps.FirstOrDefault(m => m.Name == Parameter.Map);
            if (map == null) return targets;

            // Preserve existing aliases and first-match precedence. In particular,
            // county/state follow the historical assignments; use explicit Google
            // component names in new arrangements to avoid that ambiguity.
            var aliases = new Dictionary<string, string[]> {
               ["street_number"] = ["street_number"],
               ["route"] = ["route", "street"],
               ["postal_code"] = ["postal_code"],
               ["postal_code_suffix"] = ["postal_code_suffix"],
               ["locality"] = ["locality", "city"],
               ["administrative_area_level_1"] = ["administrative_area_level_1", "county"],
               ["administrative_area_level_2"] = ["administrative_area_level_2", "state"],
               ["country"] = ["country"]
            };
            foreach (var (component, names) in aliases) {
               var match = map.Items.FirstOrDefault(item => names.Any(name => item.From.Equals(name)));
               if (match?.To is string target && !string.IsNullOrEmpty(target)) targets[component] = target;
            }
            return targets;
         }
      }
   }
}
