using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using TransformalizeModule.Models;
using TransformalizeModule.ViewModels;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.DisplayManagement.Notify;

namespace TransformalizeModule.Drivers {
   public class TransformalizeReportPartDisplayDriver : ContentPartDisplayDriver<TransformalizeReportPart> {

      private readonly IStringLocalizer<TransformalizeReportPartDisplayDriver> S;
      private readonly IHtmlLocalizer<TransformalizeReportPartDisplayDriver> H;
      private readonly INotifier _notifier;

      public TransformalizeReportPartDisplayDriver(
         IStringLocalizer<TransformalizeReportPartDisplayDriver> localizer,
         IHtmlLocalizer<TransformalizeReportPartDisplayDriver> htmlLocalizer,
         INotifier notifier
      ) {
         S = localizer;
         H = htmlLocalizer;
         _notifier = notifier;
      }

      // change display for admin content items view?
      // public override IDisplayResult Display(TransformalizeReportPart part) => View(nameof(TransformalizeReportPart), part);

      public override IDisplayResult Edit(TransformalizeReportPart part) {
         return Initialize<EditTransformalizeReportPartViewModel>("TransformalizeReportPart_Edit", model => {
            // it did not work out when i tried to flatten model (e.g. BulkActions.Value => BulkActions (a bool))
            model.TransformalizeReportPart = part;
            model.Arrangement = part.Arrangement;
            model.PageSizes = part.PageSizes;
            model.BulkActions = part.BulkActions;
            model.BulkActionValueField = part.BulkActionValueField;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeReportPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         // this driver override makes sure all the 
         // part fields are updated before the arrangement model is updated / validated

         //_notifier.Information(H["Part - Bulk Actions:{0}", part.BulkActions.Value]);
         //_notifier.Information(H["Part - Bulk Action Field:{0}", part.BulkActionValueField.Text]);

         var model = new EditTransformalizeReportPartViewModel();

         if (await updater.TryUpdateModelAsync(model, Prefix, m => m.BulkActions, m => m.BulkActionValueField)) {
            //_notifier.Information(H["Model - Bulk Actions:{0}", model.BulkActions.Value]);
            //_notifier.Information(H["Model - Bulk Action Field:{0}", model.BulkActionValueField.Text]);

            part.BulkActions.Value = model.BulkActions.Value;
            part.BulkActionValueField.Text = model.BulkActionValueField.Text;
         }

         if (model.BulkActions.Value) {
            if (string.IsNullOrEmpty(model.BulkActionValueField.Text)) {
               updater.ModelState.AddModelError(Prefix, S["Please set the bulk action value field for bulk actions."]);
            }
         }

         return Edit(part, context);

      }


   }
}
