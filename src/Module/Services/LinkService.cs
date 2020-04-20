using Flurl;
using Module.Services.Contracts;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Module.Services {
   public class LinkService : ILinkService {
      public HtmlString Create(HttpRequest request, ISession session, string contentItemId, string actionUrl, string type, bool everything) {

         var url = RemoveNoiseFromUrl(request.GetDisplayUrl().SetQueryParam("output", type));
         url.Path = actionUrl;

         if (everything) {
            url.SetQueryParam("page", 0);
         } else {
            if (request.Query["size"].ToString() == null) {
               // url.SetQueryParam("size", Common.GetStickyParameter(request, session, contentItemId, "size", () => 20));
            }
         }

         switch (type) {
            case "report":
               url.RemoveQueryParam("output");
               return new HtmlString(url);
            default:
               return new HtmlString(url.SetQueryParam("output", type).ToString());
         }

      }

      private static Url RemoveNoiseFromUrl(Url url) {

         var stars = (from param in url.QueryParams where param.Value.Equals("*") || param.Value.Equals("") select param.Name).ToList();
         foreach (var star in stars) {
            url.QueryParams.Remove(star);
         }

         return url;
      }
   }
}
