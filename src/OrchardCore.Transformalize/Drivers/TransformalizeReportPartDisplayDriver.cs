using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.TransformalizeModule.Models;
using OrchardCore.TransformalizeModule.ViewModels;

namespace OrchardCore.TransformalizeModule.Drivers {
   public class TransformalizeReportPartDisplayDriver : ContentPartDisplayDriver<TransformalizeReportPart> {

      private readonly IStringLocalizer S;

      public TransformalizeReportPartDisplayDriver(
         IStringLocalizer<TransformalizeReportPartDisplayDriver> localizer
      ) {
         S = localizer;
      }

      // change display for admin content items view?
      // public override IDisplayResult Display(TransformalizeReportPart part) => View(nameof(TransformalizeReportPart), part);

      public override IDisplayResult Edit(TransformalizeReportPart part) {
         return Initialize<EditTransformalizeReportPartViewModel>("TransformalizeReportPart_Edit", model => {
            model.TransformalizeReportPart = part;
            model.Arrangement = part.Arrangement.Arrangement;
            model.PageSizes = part.PageSizes.PageSizes;
            model.BulkActions = part.BulkActions.Value;
            model.BulkActionValueField = part.BulkActionValueField.Text;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeReportPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         // this driver override makes sure all the 
         // part fields are updated before the arrangement model is updated / validated

         var model = new EditTransformalizeReportPartViewModel();

         await updater.TryUpdateModelAsync(model, Prefix, m => m.BulkActions, m => m.BulkActionValueField);

         if (model.BulkActions) {
            if (string.IsNullOrEmpty(model.BulkActionValueField)) {
               updater.ModelState.AddModelError(Prefix, S["Please set the bulk action value field for bulk actions."]);
            }
         }

         part.BulkActions.Value = model.BulkActions;
         part.BulkActionValueField.Text = model.BulkActionValueField;

         return Edit(part, context);

      }


   }
}
