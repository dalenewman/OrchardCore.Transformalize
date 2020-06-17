using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Module.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using Module.Fields;
using Transformalize.Configuration;
using System.Linq;
using System;
using Module.Models;
using Module.Services;
using Transformalize.Logging;
using Transformalize.Contracts;
using System.Collections.Generic;
using Autofac;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Module.Drivers {
   
   public class TransformalizeArrangementFieldDisplayDriver : ContentFieldDisplayDriver<TransformalizeArrangementField> {

      private readonly IStringLocalizer<TransformalizeArrangementFieldDisplayDriver> S;
      private readonly IHtmlLocalizer<TransformalizeArrangementFieldDisplayDriver> H;
      private readonly IConfigurationContainer _container;
      private readonly INotifier _notifier;

      public TransformalizeArrangementFieldDisplayDriver(
         IStringLocalizer<TransformalizeArrangementFieldDisplayDriver> localizer,
         IHtmlLocalizer<TransformalizeArrangementFieldDisplayDriver> htmlLocalizer,
         IConfigurationContainer container,
         INotifier notifier
      ) {
         S = localizer;
         H = htmlLocalizer;
         _container = container;
         _notifier = notifier;
      }

      public override IDisplayResult Display(TransformalizeArrangementField field, BuildFieldDisplayContext context) {
         var shapeType = GetDisplayShapeType(context);
         return Initialize<DisplayTransformalizeArrangementFieldViewModel>(shapeType, model => {
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

            var displayName = context.PartFieldDefinition.DisplayName();

            if (string.IsNullOrWhiteSpace(field.Arrangement)) {
               updater.ModelState.AddModelError(Prefix, S["A value is required for {0}.", displayName]);
            } else {
               try {
                  var logger = new MemoryLogger(LogLevel.Error);
                  var process = _container.CreateScope(field.Arrangement, logger, new Dictionary<string, string>()).Resolve<Process>();
                  if (process.Errors().Any()) {
                     foreach (var error in process.Errors()) {
                        updater.ModelState.AddModelError(Prefix, S[error]);
                     }
                  }
                  if (process.Warnings().Any()) {
                     foreach(var warning in process.Warnings()) {
                        _notifier.Warning(H[warning]);
                     }
                  }
                  if(context.ContentPart is TransformalizeReportPart part) {
                     if (process.Entities.Any()) {
                        if (part.BulkActions.Value) {
                           if (!string.IsNullOrEmpty(part.BulkActionValueField.Text)) {
                              if (process.Entities[0].GetAllFields().All(f => f.Alias != part.BulkActionValueField.Text)) {
                                 updater.ModelState.AddModelError(Prefix, S["The field {0} does not exist in {1}.", part.BulkActionValueField.Text, displayName]);
                              }
                           }
                        }
                     } else {
                        updater.ModelState.AddModelError(Prefix, S["Please define an entity in {0}.", displayName]);
                     }
                  }
               } catch (Exception ex) {
                  updater.ModelState.AddModelError(Prefix, S[ex.Message]);
               }
            }

         }

         return Edit(field, context);
      }
   }
}
