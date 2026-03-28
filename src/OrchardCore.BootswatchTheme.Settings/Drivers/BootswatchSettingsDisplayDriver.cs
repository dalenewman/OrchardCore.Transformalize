using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Cache;
using OrchardCore.Settings;
using OrchardCore.BootswatchTheme.Settings.Models;
using OrchardCore.BootswatchTheme.Settings.ViewModels;

namespace OrchardCore.BootswatchTheme.Settings.Drivers {
   public class BootswatchSettingsDisplayDriver : SectionDisplayDriver<ISite, BootswatchSettings> {

      private readonly IAuthorizationService _authorizationService;
      private readonly IHttpContextAccessor _hca;
      private readonly ISignal _signal;

      public BootswatchSettingsDisplayDriver(
            IAuthorizationService authorizationService,
            IHttpContextAccessor hca,
            ISignal signal) {
         _authorizationService = authorizationService;
         _hca = hca;
         _signal = signal;
      }

      public override async Task<IDisplayResult?> EditAsync(ISite model, BootswatchSettings settings, BuildEditorContext context) {

         if (!await IsAuthorizedAsync()) {
            return null;
         }

         return Initialize<BootswatchSettingsViewModel>($"{nameof(BootswatchSettings)}_Edit", vm => {
            vm.Theme = settings.Theme;
            vm.AvailableThemes = BuildThemeList(settings.Theme);
         })
         .Location("Content:1")
         .OnGroup(Constants.SettingsGroupId);
      }

      public override async Task<IDisplayResult?> UpdateAsync(ISite model, BootswatchSettings settings, UpdateEditorContext context) {

         if (context.GroupId == Constants.SettingsGroupId) {

            if (!await IsAuthorizedAsync()) {
               return null;
            }

            var vm = new BootswatchSettingsViewModel();
            await context.Updater.TryUpdateModelAsync(vm, Prefix);

            if (settings.Theme != vm.Theme) {
               settings.Theme = vm.Theme;
               await _signal.SignalTokenAsync(Constants.CacheKey);
            }
         }

         return await EditAsync(model, settings, context);
      }

      private Task<bool> IsAuthorizedAsync() =>
         _authorizationService.AuthorizeAsync(_hca.HttpContext!.User, Permissions.ManageBootswatchSettings);

      private static IEnumerable<SelectListItem> BuildThemeList(string currentTheme) {
         var items = new List<SelectListItem> {
            new SelectListItem("Default (Bootstrap)", "default", currentTheme == "default")
         };
         items.AddRange(Constants.BootswatchThemes.Select(t =>
            new SelectListItem(
               char.ToUpper(t[0]) + t[1..],
               t,
               t == currentTheme)));
         return items;
      }
   }
}
