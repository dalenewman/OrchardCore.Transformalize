using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Module.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;
using Module.Fields;
using Module.Models;
using OrchardCore.DisplayManagement.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Contents;

namespace Module.Drivers {
   public class TransformalizeSettingsDisplayDriver : SectionDisplayDriver<ISite, TransformalizeSettings> {

      private readonly IAuthorizationService _authorizationService;
      private readonly IHttpContextAccessor _hca;

      public TransformalizeSettingsDisplayDriver(IAuthorizationService authorizationService, IHttpContextAccessor hca) {
         _authorizationService = authorizationService;
         _hca = hca;
      }

      // Here's the EditAsync override to display editor for our site settings on the Dashboard.
      public override async Task<IDisplayResult> EditAsync(TransformalizeSettings settings, BuildEditorContext context) {

         if (!await IsAuthorizedToManageTransformalizeSettingsAsync()) {
            return null;
         }

         return Initialize<TransformalizeSettingsViewModel>($"{nameof(TransformalizeSettings)}_Edit", model => {
            model.CommonArrangement = settings.CommonArrangement;
         })
         .Location("Content:1")
         .OnGroup(Common.SettingsGroupId);


      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeSettings settings, BuildEditorContext context) {

         if (context.GroupId == Common.SettingsGroupId) {

            if (!await IsAuthorizedToManageTransformalizeSettingsAsync()) {
               return null;
            }

            // Update the view model and the model as usual.
            var model = new TransformalizeSettingsViewModel();

            await context.Updater.TryUpdateModelAsync(model, Prefix);

            settings.CommonArrangement = model.CommonArrangement;
         }

         return await EditAsync(settings, context);
      }

      private async Task<bool> IsAuthorizedToManageTransformalizeSettingsAsync() {
         var user = _hca.HttpContext?.User;

         return user != null && await _authorizationService.AuthorizeAsync(user, Permissions.ManageTransformalizeSettings );
      }

   }
}
