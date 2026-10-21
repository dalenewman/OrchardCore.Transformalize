using Flurl;
using TransformalizeModule.Services.Contracts;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using TransformalizeModule.Models;

namespace TransformalizeModule.Services {
   public class LinkService : ILinkService {
      private readonly IHttpContextAccessor _contextAccessor;

      public LinkService(IHttpContextAccessor contextAccessor) {
         _contextAccessor = contextAccessor;
      }

      public HtmlString Create(string contentItemId, string actionUrl, bool everything, ReportPaging? paging = null) {

         var url = RemoveNoiseFromUrl(_contextAccessor.HttpContext.Request.GetDisplayUrl());
         url.Path = actionUrl;

         if (paging != null) {
            var size = paging.SizeParameter(_contextAccessor.HttpContext.Request.Query["size"].ToString());
            if (size.Length == 0) url.RemoveQueryParam("size");
            else url.SetQueryParam("size", size);
         }

         if (everything) {
            url.SetQueryParam("page", 0);
         } else {
            if (_contextAccessor.HttpContext.Request.Query.ContainsKey("size")) {
               var qSize = _contextAccessor.HttpContext.Request.Query["size"].ToString() ?? string.Empty;
               if (!int.TryParse(qSize, out _)) {
                  url.RemoveQueryParam("size");
               }
            }
            if (_contextAccessor.HttpContext.Request.Query.ContainsKey("page")) {
               var qPage = _contextAccessor.HttpContext.Request.Query["page"].ToString() ?? string.Empty;
               if (int.TryParse(qPage, out int page)) {
                  if(page <= 1) {
                     url.RemoveQueryParam("page");
                  }
               } else {
                  url.RemoveQueryParam("page");
               }
            }
         }

         return new HtmlString(url);

      }

      internal static Url RemoveNoiseFromUrl(Url url) {

         foreach (var group in url.QueryParams.GroupBy(param => param.Name).ToArray()) {
            var kept = group.Where(param => !string.IsNullOrWhiteSpace(param.Value?.ToString())
               && param.Value.ToString()!.Trim() != "*").ToArray();
            if (kept.Length == group.Count()) continue;
            url.QueryParams.Remove(group.Key);
            foreach (var param in kept) url.QueryParams.Add(param.Name, param.Value);
         }

         return url;
      }
   }
}
