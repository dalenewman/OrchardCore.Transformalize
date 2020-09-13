using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Logging;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Drivers {
   public class TransformalizeFormPartDisplayDriver : ContentPartDisplayDriver<TransformalizeFormPart> {

      private readonly IStringLocalizer S;
      private readonly IHtmlLocalizer<TransformalizeFormPartDisplayDriver> H;
      private readonly IConfigurationContainer _container;
      private readonly INotifier _notifier;

      public TransformalizeFormPartDisplayDriver(
         IStringLocalizer<TransformalizeFormPartDisplayDriver> localizer,
         IHtmlLocalizer<TransformalizeFormPartDisplayDriver> htmlLocalizer,
         IConfigurationContainer container,
         INotifier notifier
      ) {
         S = localizer;
         H = htmlLocalizer;
         _container = container;
         _notifier = notifier;
      }

      public override IDisplayResult Edit(TransformalizeFormPart part) {
         return Initialize<EditTransformalizeFormPartViewModel>("TransformalizeFormPart_Edit", model => {
            model.TransformalizeFormPart = part;
            model.Arrangement = part.Arrangement;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(TransformalizeFormPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         var model = new EditTransformalizeFormPartViewModel();

         if (await updater.TryUpdateModelAsync(model, Prefix)) {
            part.Arrangement.Text = model.Arrangement.Text;
         }

         try {
            var logger = new MemoryLogger(LogLevel.Error);
            var process = _container.CreateScope(model.Arrangement.Text, part.ContentItem, new Dictionary<string, string>(), false).Resolve<Process>();
            if (process.Errors().Any()) {
               foreach (var error in process.Errors()) {
                  updater.ModelState.AddModelError(Prefix, S[error]);
               }
            }
            
            if (process.Warnings().Any()) {
               foreach (var warning in process.Warnings()) {
                  _notifier.Warning(H[warning]);
               }
            }

            if(!process.Connections.Any(c=>c.Table != "[default]" && !string.IsNullOrEmpty(c.Table))) {
               updater.ModelState.AddModelError(Prefix, S["A form requires one of the connections to have a table.  The form submissions are stored in the specified table."]);
            }

            if (!process.Parameters.Any(p => p.PrimaryKey)) {
               updater.ModelState.AddModelError(Prefix, S["A form requires one of the parameters to be marked as the primary key."]);
            }

         } catch (Exception ex) {
            updater.ModelState.AddModelError(Prefix, S[ex.Message]);
         }

         return Edit(part, context);

      }

   }
}
