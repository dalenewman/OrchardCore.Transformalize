using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Module.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using Module.Fields;

namespace Module.Drivers {
   public class TransformalizeArrangementFieldDisplayDriver : ContentFieldDisplayDriver<TransformalizeArrangementField> {
      private readonly IStringLocalizer S;

      public TransformalizeArrangementFieldDisplayDriver(IStringLocalizer<TransformalizeArrangementFieldDisplayDriver> localizer) {
         S = localizer;
      }

      public override IDisplayResult Display(TransformalizeArrangementField field, BuildFieldDisplayContext context) {
         return Initialize<DisplayTransformalizeArrangementFieldViewModel>(GetDisplayShapeType(context), model => {
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
         })
         .Location("Detail", "Content")
         .Location("Summary", "Content");
      }

      public override IDisplayResult Edit(TransformalizeArrangementField field, BuildFieldEditorContext context) {
         return Initialize<EditTransformalizeArrangementFieldViewModel>(GetEditorShapeType(context), model => {
            model.Arrangement = field.Arrangement;
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
         });
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeArrangementField field, IUpdateModel updater, UpdateFieldEditorContext context) {
         if (await updater.TryUpdateModelAsync(field, Prefix, f => f.Arrangement)) {
            if (String.IsNullOrWhiteSpace(field.Arrangement)) {
               updater.ModelState.AddModelError(Prefix, S["A value is required for {0}.", context.PartFieldDefinition.DisplayName()]);
            }
         }

         return Edit(field, context);
      }
   }
}
