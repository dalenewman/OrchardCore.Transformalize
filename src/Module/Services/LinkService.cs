using Flurl;
using Module.Services.Contracts;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Module.Services {
   public class LinkService : ILinkService {
      private readonly IHttpContextAccessor _contextAccessor;
      private readonly IStickyParameterService _stickyParameterService;

      public LinkService(IHttpContextAccessor contextAccessor, IStickyParameterService stickyParameterService) {
         _contextAccessor = contextAccessor;
         _stickyParameterService = stickyParameterService;
      }

      public HtmlString Create(string contentItemId, string actionUrl, bool everything) {

         var url = RemoveNoiseFromUrl(_contextAccessor.HttpContext.Request.GetDisplayUrl());
         url.Path = actionUrl;

         if (everything) {
            url.SetQueryParam("page", 0);
         } else {
            if (_contextAccessor.HttpContext.Request.Query["size"].ToString() == null) {
               url.SetQueryParam("size", _stickyParameterService.GetStickyParameter(contentItemId, "size", () => 20));
            }
         }

         return new HtmlString(url);

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
