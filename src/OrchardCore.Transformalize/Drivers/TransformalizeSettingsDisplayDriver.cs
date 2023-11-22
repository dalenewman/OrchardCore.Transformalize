using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;
using Transformalize.Configuration;
using TransformalizeModule.Models;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Drivers {
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
            model.DefaultPageSizesExtended = settings.DefaultPageSizesExtended;
            model.MapBoxToken = settings.MapBoxToken;
            model.GoogleApiKey = settings.GoogleApiKey;

            model.BulkActionCreateTask = settings.BulkActionCreateTask;
            model.BulkActionWriteTask = settings.BulkActionWriteTask;
            model.BulkActionSummaryTask = settings.BulkActionSummaryTask;
            model.BulkActionRunTask = settings.BulkActionRunTask;
            model.BulkActionSuccessTask = settings.BulkActionSuccessTask;
            model.BulkActionFailTask = settings.BulkActionFailTask;
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
            settings.GoogleApiKey = model.GoogleApiKey;

            settings.BulkActionCreateTask = model.BulkActionCreateTask;
            settings.BulkActionWriteTask = model.BulkActionWriteTask;
            settings.BulkActionSummaryTask = model.BulkActionSummaryTask;
            settings.BulkActionRunTask = model.BulkActionRunTask;
            settings.BulkActionSuccessTask = model.BulkActionSuccessTask;
            settings.BulkActionFailTask = model.BulkActionFailTask;

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

            if (PageSizesOkay(context, "Default Page Sizes", model.DefaultPageSizes)) {
               settings.DefaultPageSizes = model.DefaultPageSizes;
            }

            if (PageSizesOkay(context, "Default Page Sizes Extended", model.DefaultPageSizesExtended)) {
               settings.DefaultPageSizesExtended = model.DefaultPageSizesExtended;
            }

         }

         return await EditAsync(settings, context);
      }

      private async Task<bool> IsAuthorizedToManageTransformalizeSettingsAsync() {
         var user = _hca.HttpContext?.User;

         return user != null && await _authorizationService.AuthorizeAsync(user, Permissions.ManageTransformalizeSettings);
      }

      private bool PageSizesOkay(BuildEditorContext context, string name, string value) {
         if (string.IsNullOrWhiteSpace(value)) {
            context.Updater.ModelState.AddModelError(Prefix, S["{0} must be a comma delimited list of integers.", name]);
            return false;
         } else {
            var clean = true;
            foreach (var size in value.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
               if (!int.TryParse(size, out int result)) {
                  context.Updater.ModelState.AddModelError(Prefix, S["{0} value {1} is not a valid integer.", name, size]);
                  clean = false;
               }
            }
            return clean;
         }
      }

   }
}
