using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using TransformalizeModule.Models;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Drivers {
   public class TransformalizeTaskPartDisplayDriver : ContentPartDisplayDriver<TransformalizeTaskPart> {

      private readonly IStringLocalizer S;

      public TransformalizeTaskPartDisplayDriver(
         IStringLocalizer<TransformalizeTaskPartDisplayDriver> localizer
      ) {
         S = localizer;
      }

      public override IDisplayResult Edit(TransformalizeTaskPart part) {
         return Initialize<EditTransformalizeTaskPartViewModel>("TransformalizeTaskPart_Edit", model => {
            model.TransformalizeTaskPart = part;
            model.Arrangement = part.Arrangement.Arrangement;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeTaskPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         var model = new EditTransformalizeTaskPartViewModel();

         await updater.TryUpdateModelAsync(model, Prefix);

         // validations, add things to update.ModelState

         part.Arrangement.Arrangement = model.Arrangement;

         return Edit(part, context);

      }

   }
}
