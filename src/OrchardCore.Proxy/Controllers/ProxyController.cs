using Microsoft.AspNetCore.Mvc;
using ProxyModule.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCoreContrib.ContentPermissions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace ProxyModule.Controllers {

   public class ProxyController : Controller {

      private static readonly HashSet<string> _restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Accept",
            "Accept-Encoding",
            "Accept-Language",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent",
            "Proxy-Connection"
        };

      private static readonly Dictionary<string, Regex> _proxyTrimmers = [];
      private readonly IStringLocalizer<ProxyController> S;
      private readonly ILogger<ProxyController> _logger;
      private readonly IContentManager _contentManager;
      private readonly IContentHandleManager _aliasManager;
      private readonly IContentPermissionsService _contentPermissionsService;

      public ProxyController(
          IContentManager contentManager,
          IContentHandleManager aliasManager,
          IContentPermissionsService contentPermissionsService,
          ILogger<ProxyController> logger,
          IStringLocalizer<ProxyController> s
      ) {
         _aliasManager = aliasManager;
         _contentManager = contentManager;
         _contentPermissionsService = contentPermissionsService;
         S = s;
         _logger = logger;
      }

      [AcceptVerbs("GET", "HEAD", "POST", "PUT", "DELETE")]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await GetByIdOrAliasAsync(contentItemId);

         if (contentItem == null) {
            return new StatusCodeResult(404);
         }

         var authorized = await _contentPermissionsService.AuthorizeAsync(contentItem);
         if (!authorized) {
            return new StatusCodeResult(401);
         }

         var part = contentItem?.As<ProxyPart>();
         if (part == null) {
            return new StatusCodeResult(500);
         }

         if (!_proxyTrimmers.TryGetValue(contentItemId, out var proxyTrimmer))
         {
             proxyTrimmer = new Regex(string.Format(@"{0}/Proxy/{1}", Url.Content("~").TrimEnd('/'), contentItemId), RegexOptions.Compiled);
             _proxyTrimmers[contentItemId] = proxyTrimmer;
         }

         var url = proxyTrimmer.Replace(HttpContext.Request.Path + HttpContext.Request.QueryString, string.Empty);


         return await RelayContent(CombinePath(part.ServiceUrl.Text, url), Request, Response, _logger, S, part.ForwardHeaders.Value);
      }

      private static string CombinePath(string proxyUrl, string requestedUrl) {
         if (proxyUrl.EndsWith("/") && requestedUrl.StartsWith("/")) {
            return proxyUrl + requestedUrl.TrimStart(new[] { '/' });
         }
         return proxyUrl + requestedUrl;
      }

      private static async Task<EmptyResult> RelayContent(
          string url,
          HttpRequest request,
          HttpResponse response,
          ILogger<ProxyController> logger,
          IStringLocalizer<ProxyController> s,
          bool forwardHeaders
      ) {

         var uri = new Uri(url);
         var client = new HttpClient();
         var message = new HttpRequestMessage(new HttpMethod(request.Method), uri);

         if (forwardHeaders) {
            foreach (var key in request.Headers.Keys.Where(key => !_restrictedHeaders.Contains(key))) {
               message.Headers.Add(key, request.Headers[key].ToString());
            }
         }

         //pull in input
         if (message.Method != HttpMethod.Get) {

            request.Body.Seek(0, SeekOrigin.Begin);

            var inStream = request.Body;

            var webStream = new MemoryStream();
            try {
               //copy incoming request body to outgoing request
               if (inStream != null && inStream.Length > 0) {
                  inStream.CopyTo(webStream);
                  message.Content = new StreamContent(webStream);
                  message.Headers.Add("Content-Type", request.ContentType);
               }
            } finally {
               if (null != webStream) {
                  webStream.Flush();
                  webStream.Close();
               }
            }
         }

         //get and push output
         HttpResponseMessage resourceResponse = null;
         try {
            using (resourceResponse = await client.SendAsync(message)) {

               IEnumerable<string> responseTags;
               if (resourceResponse.Headers.TryGetValues("ETag", out responseTags)) {
                  response.Headers["Cache-Control"] = "private, must-revalidate";
                  response.Headers["ETag"] = responseTags.First();

                  IEnumerable<string> requestTags;
                  if(message.Headers.TryGetValues("If-None-Match", out requestTags) && requestTags.First() == response.Headers["ETag"]) {
                     response.StatusCode = 304;  // Not Modified
                     return new EmptyResult();
                  }
               }

               using (var resourceStream = await resourceResponse.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
                  await resourceStream.CopyToAsync(response.Body);
               }
               var cacheControl = resourceResponse.Headers.GetValues("Cache-Control").FirstOrDefault();

               if (cacheControl != null) {
                  response.Headers["Cache-Control"] = cacheControl;
               }

               response.ContentType = uri.IsFile ? MimeTypeMap.GetMimeType(Path.GetExtension(uri.LocalPath)) : resourceResponse.Headers.GetValues("Content-Type").FirstOrDefault();
            }
         } catch (HttpRequestException ex) {
            if (resourceResponse != null) {
               response.StatusCode = (int)resourceResponse.StatusCode;
            } else {
               response.StatusCode = 500;
            }
            logger.LogError(ex, s["Proxy module protocol error: {0}"], ex.Message);
         } finally {
            await response.Body.FlushAsync();
         }
         return new EmptyResult();
      }

      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         if (string.IsNullOrEmpty(idOrAlias)) {
            return null;
         }

         ContentItem contentItem = null;
         if (idOrAlias.Length == Common.IdLength) {
            contentItem = await _contentManager.GetAsync(idOrAlias);
         }
         if (contentItem == null) {
            var id = await _aliasManager.GetContentItemIdAsync("alias:" + idOrAlias);
            if (id != null) {
               contentItem = await _contentManager.GetAsync(id);
            }
         }
         return contentItem;
      }
   }
}
