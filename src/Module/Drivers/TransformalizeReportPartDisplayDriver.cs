using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using Module.Models;

namespace Module.Drivers {
   public class TransformalizeReportPartDisplayDriver : ContentPartDisplayDriver<TransformalizeReportPart> {

      private readonly IStringLocalizer S;

      public TransformalizeReportPartDisplayDriver(
         IStringLocalizer<TransformalizeReportPartDisplayDriver> localizer
      ) {
         S = localizer;
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeReportPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         // this driver override makes sure all the 
         // part fields are updated before the arrangement model is updated / validated

         await updater.TryUpdateModelAsync(part, Prefix);

         if (part.BulkActions.Value) {
            if (string.IsNullOrEmpty(part.BulkActionValueField.Text)) {
               updater.ModelState.AddModelError(Prefix, S["Please set the bulk action value field for bulk actions."]);
            }
         }

         return await base.UpdateAsync(part, updater, context);
      }


   }
}
