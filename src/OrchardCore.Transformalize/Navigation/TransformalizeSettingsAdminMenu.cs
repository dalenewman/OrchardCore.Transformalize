using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace TransformalizeModule.Navigation {
   public class TransformalizeSettingsAdminMenu : AdminNavigationProvider {
      private readonly IStringLocalizer T;

      public TransformalizeSettingsAdminMenu(IStringLocalizer<TransformalizeSettingsAdminMenu> stringLocalizer) {
         T = stringLocalizer;
      }

      protected override ValueTask BuildAsync(NavigationBuilder builder) { 

         builder.Add(T["Transformalize"], configuration => configuration
            .Id("tfl")
            .AddClass("tfl")
            .Add(T["Settings"], settings => settings
               .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = Common.SettingsGroupId })
               .Permission(Permissions.ManageTransformalizeSettings)
               .LocalNav()
            )
         );

         return ValueTask.CompletedTask;
      }

   }
}
