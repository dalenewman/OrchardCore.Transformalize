using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.BootswatchTheme.Settings.ViewModels {
   public class BootswatchSettingsViewModel {
      public string Theme { get; set; } = "default";
      public IEnumerable<SelectListItem> AvailableThemes { get; set; } = Enumerable.Empty<SelectListItem>();
   }
}
