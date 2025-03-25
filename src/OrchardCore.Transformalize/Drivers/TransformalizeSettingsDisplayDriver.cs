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
      public override async Task<IDisplayResult> EditAsync(ISite model, TransformalizeSettings settings, BuildEditorContext context) {

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

      public override async Task<IDisplayResult> UpdateAsync(ISite model, TransformalizeSettings settings, UpdateEditorContext context) {

         if (context.GroupId == Common.SettingsGroupId) {

            if (!await IsAuthorizedToManageTransformalizeSettingsAsync()) {
               return null;
            }

            // this gets what's coming from the editor into model
            var viewModel = new TransformalizeSettingsViewModel();

            await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

            settings.MapBoxToken = viewModel.MapBoxToken;
            settings.GoogleApiKey = viewModel.GoogleApiKey;

            settings.BulkActionCreateTask = viewModel.BulkActionCreateTask;
            settings.BulkActionWriteTask = viewModel.BulkActionWriteTask;
            settings.BulkActionSummaryTask = viewModel.BulkActionSummaryTask;
            settings.BulkActionRunTask = viewModel.BulkActionRunTask;
            settings.BulkActionSuccessTask = viewModel.BulkActionSuccessTask;
            settings.BulkActionFailTask = viewModel.BulkActionFailTask;

            // common arrangement
            if (string.IsNullOrWhiteSpace(viewModel.CommonArrangement)) {
               settings.CommonArrangement = viewModel.CommonArrangement;
            } else {
               try {
                  var process = new Process(viewModel.CommonArrangement);
                  if (process.Errors().Any()) {
                     foreach (var error in process.Errors()) {
                        context.Updater.ModelState.AddModelError(Prefix, S[error]);
                     }
                  } else {
                     settings.CommonArrangement = viewModel.CommonArrangement;
                  }
               } catch (Exception ex) {
                  context.Updater.ModelState.AddModelError(Prefix, S[ex.Message]);
               }
            }

            if (PageSizesOkay(context, "Default Page Sizes", viewModel.DefaultPageSizes)) {
               settings.DefaultPageSizes = viewModel.DefaultPageSizes;
            }

            if (PageSizesOkay(context, "Default Page Sizes Extended", viewModel.DefaultPageSizesExtended)) {
               settings.DefaultPageSizesExtended = viewModel.DefaultPageSizesExtended;
            }

         }

         return await EditAsync(model, settings, context);
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
