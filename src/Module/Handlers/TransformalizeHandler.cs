using System.Threading.Tasks;
using OrchardCore.Alias.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;

namespace Module.Handlers {
   public class TransformalizeHandler : ContentHandlerBase {

      public override Task GetContentItemAspectAsync(ContentItemAspectContext context) {
         // just changes the view link

         if (context.ContentItem.ContentType == "TransformalizeReport") {
            return context.ForAsync<ContentItemMetadata>(metadata => {
               if (metadata.DisplayRouteValues != null) {
                  metadata.DisplayRouteValues.Remove("Area");
                  metadata.DisplayRouteValues.Add("Area", Common.ModuleName);
                  metadata.DisplayRouteValues.Remove("Controller");
                  metadata.DisplayRouteValues.Add("Controller", "Report");
                  metadata.DisplayRouteValues.Remove("Action");
                  metadata.DisplayRouteValues.Add("Action", "Index");
                  metadata.DisplayRouteValues.Remove("ContentItemId");
                  metadata.DisplayRouteValues.Add("ContentItemId", context.ContentItem.As<AliasPart>().Alias);
               }
               return Task.CompletedTask;
            });
         } else {
            return Task.CompletedTask;
         }
      }
   }
}
