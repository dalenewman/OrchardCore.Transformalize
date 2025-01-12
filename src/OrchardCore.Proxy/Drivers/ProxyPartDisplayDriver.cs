using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
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

      public override IDisplayResult Edit(ProxyPart part, BuildPartEditorContext context) {
         return Initialize<EditProxyPartViewModel>("ProxyPart_Edit", model => {
            model.ProxyPart = part;
            model.ServiceUrl = part.ServiceUrl;
            model.ForwardHeaders = part.ForwardHeaders;
         }).Location("Content:1");
      }

      public override async Task<IDisplayResult> UpdateAsync(ProxyPart part, UpdatePartEditorContext context) {

         // this driver override makes sure all the 
         // part fields are updated before the arrangement model is updated / validated

         var model = new EditProxyPartViewModel {
            ProxyPart = part
         };

         if (await context.Updater.TryUpdateModelAsync(model, Prefix)) {
            if (model.ServiceUrl != null) {
               part.ServiceUrl.Text = model.ServiceUrl.Text;
            }
            if (model.ForwardHeaders != null) {
               part.ForwardHeaders.Value = model.ForwardHeaders.Value;
            }
         }

         if (model.ServiceUrl != null && !IsValidUri(model.ServiceUrl.Text)) {
            context.Updater.ModelState.AddModelError(Prefix, S["Please set service url to a valid absolute url."]);
         }

         return Edit(part, context);

      }

      public static bool IsValidUri(string uri) {
         return Uri.TryCreate(uri, UriKind.Absolute, out _);
      }
   }

}
