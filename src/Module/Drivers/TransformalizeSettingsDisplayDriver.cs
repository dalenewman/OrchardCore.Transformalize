using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Module.ViewModels;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;
using Module.Models;
using OrchardCore.DisplayManagement.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Handlers;
using Transformalize.Configuration;
using System.Linq;

namespace Module.Drivers {
   public class TransformalizeSettingsDisplayDriver : SectionDisplayDriver<ISite, TransformalizeSettings> {

      private readonly IAuthorizationService _authorizationService;
      private readonly IHttpContextAccessor _hca;
      private readonly IStringLocalizer S;

      public TransformalizeSettingsDisplayDriver(
            IAuthorizationService authorizationService, 
            IHttpContextAccessor hca,
            IStringLocalizer<TransformalizeSettingsDisplayDriver> localizer) {
         _authorizationService = authorizationService;
         _hca = hca;
         S = localizer;
      }

      // Here's the EditAsync override to display editor for our site settings on the Dashboard.
      public override async Task<IDisplayResult> EditAsync(TransformalizeSettings settings, BuildEditorContext context) {

         if (!await IsAuthorizedToManageTransformalizeSettingsAsync()) {
            return null;
         }

         return Initialize<TransformalizeSettingsViewModel>($"{nameof(TransformalizeSettings)}_Edit", model => {
            model.CommonArrangement = settings.CommonArrangement;
            model.DefaultPageSizes = settings.DefaultPageSizes;
            model.MapBoxToken = settings.MapBoxToken;
         })
         .Location("Content:1")
         .OnGroup(Common.SettingsGroupId);

      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeSettings settings, BuildEditorContext context) {

         if (context.GroupId == Common.SettingsGroupId) {

            if (!await IsAuthorizedToManageTransformalizeSettingsAsync()) {
               return null;
            }

            // this gets what's coming from the editor into model
            var model = new TransformalizeSettingsViewModel();
            await context.Updater.TryUpdateModelAsync(model, Prefix);

            settings.MapBoxToken = model.MapBoxToken;

            // common arrangement
            if (string.IsNullOrWhiteSpace(model.CommonArrangement)) {
               settings.CommonArrangement = model.CommonArrangement;
            } else {
               try {
                  var process = new Process(model.CommonArrangement);
                  if (process.Errors().Any()) {
                     foreach (var error in process.Errors()) {
                        context.Updater.ModelState.AddModelError(Prefix, S[error]);
                     }
                  } else {
                     settings.CommonArrangement = model.CommonArrangement;
                  }
               } catch (Exception ex) {
                  context.Updater.ModelState.AddModelError(Prefix, S[ex.Message]);
               }
            }

            // default page sizes
            if (string.IsNullOrWhiteSpace(model.DefaultPageSizes)) {
               context.Updater.ModelState.AddModelError(Prefix, S["Default Page Sizes must be a comma delimited list of integers."]);
            } else {
               var clean = true;
               foreach (var size in model.DefaultPageSizes.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                  if (!int.TryParse(size, out int result)) {
                     context.Updater.ModelState.AddModelError(Prefix, S["Default Page Sizes value {0} is not a valid integer.",  size]);
                     clean = false;
                  }
               }
               if (clean) {
                  settings.DefaultPageSizes = model.DefaultPageSizes;
               }
            }
         }

         return await EditAsync(settings, context);
      }

      private async Task<bool> IsAuthorizedToManageTransformalizeSettingsAsync() {
         var user = _hca.HttpContext?.User;

         return user != null && await _authorizationService.AuthorizeAsync(user, Permissions.ManageTransformalizeSettings);
      }

   }
}
