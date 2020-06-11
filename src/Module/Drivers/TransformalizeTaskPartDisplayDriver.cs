using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using Module.Models;
using Module.ViewModels;

namespace Module.Drivers {
   public class TransformalizeTaskPartDisplayDriver : ContentPartDisplayDriver<TransformalizeTaskPart> {

      private readonly IStringLocalizer S;

      public TransformalizeTaskPartDisplayDriver(
         IStringLocalizer<TransformalizeTaskPartDisplayDriver> localizer
      ) {
         S = localizer;
      }

      public override IDisplayResult Edit(TransformalizeTaskPart transformalizeTaskPart) {
         return Initialize<EditTransformalizeTaskPartViewModel>("TransformalizeTaskPart_Edit", model => {
            model.TransformalizeTaskPart = transformalizeTaskPart;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeTaskPart part, IUpdateModel updater, UpdatePartEditorContext context) {
         await updater.TryUpdateModelAsync(part, Prefix);
         return await base.UpdateAsync(part, updater, context);
      }

   }
}
