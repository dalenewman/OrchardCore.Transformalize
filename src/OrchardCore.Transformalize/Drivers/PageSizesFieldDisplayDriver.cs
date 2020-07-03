using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.TransformalizeModule.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.TransformalizeModule.Fields;

namespace OrchardCore.TransformalizeModule.Drivers {
   public class PageSizesFieldDisplayDriver : ContentFieldDisplayDriver<PageSizesField> {
      private readonly IStringLocalizer S;

      public PageSizesFieldDisplayDriver(
         IStringLocalizer<PageSizesFieldDisplayDriver> localizer
         ) {
         S = localizer;
      }

      public override IDisplayResult Display(PageSizesField field, BuildFieldDisplayContext context) {
         var shapeType = GetDisplayShapeType(context);
         return Initialize<DisplayPageSizesFieldViewModel>(shapeType, model => {
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
         })
         .Location("Detail", "Content")
         .Location("Summary", "Content");
      }

      public override IDisplayResult Edit(PageSizesField field, BuildFieldEditorContext context) {
         return Initialize<EditPageSizesFieldViewModel>(GetEditorShapeType(context), model => {
            model.PageSizes = field.PageSizes;
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
         });
      }

      public override async Task<IDisplayResult> UpdateAsync(PageSizesField field, IUpdateModel updater, UpdateFieldEditorContext context) {
         if (await updater.TryUpdateModelAsync(field, Prefix, f => f.PageSizes)) {

            if (string.IsNullOrWhiteSpace(field.PageSizes)) {
               field.PageSizes = string.Empty;
            } else {
               foreach(var size in field.PageSizes.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                  if(!int.TryParse(size, out int result)) {
                     updater.ModelState.AddModelError(Prefix, S["{0} value {1} is not a valid integer.", context.PartFieldDefinition.DisplayName(), size]);
                  }
               }
            }
         }

         return Edit(field, context);
      }
   }
}
