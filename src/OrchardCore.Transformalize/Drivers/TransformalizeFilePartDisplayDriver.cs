using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using TransformalizeModule.Models;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Drivers {
   public class TransformalizeFilePartDisplayDriver : ContentPartDisplayDriver<TransformalizeFilePart> {

      public TransformalizeFilePartDisplayDriver() {}

      public override IDisplayResult Edit(TransformalizeFilePart part, BuildPartEditorContext context) {
         return Initialize<EditTransformalizeFilePartViewModel>("TransformalizeFilePart_Edit", model => {
            model.TransformalizeFilePart = part;
            model.OriginalName = part.OriginalName;
            model.FullPath = part.FullPath;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeFilePart part, UpdatePartEditorContext context) {

         var model = new EditTransformalizeFilePartViewModel {
            TransformalizeFilePart = part
         };

         if (await context.Updater.TryUpdateModelAsync(model, Prefix)) {
            part.OriginalName.Text = model.OriginalName.Text;
            part.FullPath.Text = model.FullPath.Text;
         }

         return Edit(part, context);

      }

   }
}
