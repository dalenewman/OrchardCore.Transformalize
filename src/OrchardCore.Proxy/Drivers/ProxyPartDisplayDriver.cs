using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Cache;
using ProxyModule.Models;
using ProxyModule.ViewModels;

namespace ProxyModule.Drivers {
   public class ProxyPartDisplayDriver : ContentPartDisplayDriver<ProxyPart> {

      private readonly IStringLocalizer<ProxyPartDisplayDriver> S;
      private readonly IHtmlLocalizer<ProxyPartDisplayDriver> H;

      public ProxyPartDisplayDriver(
         IStringLocalizer<ProxyPartDisplayDriver> localizer,
         IHtmlLocalizer<ProxyPartDisplayDriver> htmlLocalizer
      ) {
         S = localizer;
         H = htmlLocalizer;
      }

      public override IDisplayResult Edit(ProxyPart part) {
         return Initialize<EditProxyPartViewModel>("ProxyPart_Edit", model => {
            model.ProxyPart = part;
            model.ServiceUrl = part.ServiceUrl;
            model.ForwardHeaders = part.ForwardHeaders;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(ProxyPart part, IUpdateModel updater, UpdatePartEditorContext context) {

         // this driver override makes sure all the 
         // part fields are updated before the arrangement model is updated / validated

         var model = new EditProxyPartViewModel();

         if (await updater.TryUpdateModelAsync(model, Prefix)) {

            part.ServiceUrl.Text = model.ServiceUrl.Text;
            part.ForwardHeaders.Value = model.ForwardHeaders.Value;
         }

         // TODO: validate that ServiceUrl is valid URL

         return Edit(part, context);

      }

   }
}
