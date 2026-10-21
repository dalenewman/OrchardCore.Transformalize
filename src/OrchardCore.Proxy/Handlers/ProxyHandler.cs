using OrchardCore.Alias.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;

namespace ProxyModule.Handlers {
   public class ProxyHandler : ContentHandlerBase {

      public override Task GetContentItemAspectAsync(ContentItemAspectContext context) {

         // change the view links to goto custom controllers

         switch (context.ContentItem.ContentType) {
            case "Proxy":
               return context.ForAsync<ContentItemMetadata>(metadata => {
                  if (metadata.DisplayRouteValues != null) {
                     metadata.DisplayRouteValues.Remove("Area");
                     metadata.DisplayRouteValues.Add("Area", Common.ModuleName);
                     metadata.DisplayRouteValues.Remove("Controller");
                     metadata.DisplayRouteValues.Add("Controller", "Proxy");
                     metadata.DisplayRouteValues.Remove("Action");
                     metadata.DisplayRouteValues.Add("Action", "Index");
                     metadata.DisplayRouteValues.Remove("ContentItemId");
                     var alias = context.ContentItem.TryGet<AliasPart>(out var aliasPart) ? aliasPart.Alias : null;
                     metadata.DisplayRouteValues.Add("ContentItemId", alias ?? context.ContentItem.ContentItemId);
                  }
                  return Task.CompletedTask;
               });
            default:
               return Task.CompletedTask;
         }
      }
   }
}
