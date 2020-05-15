using Etch.OrchardCore.ContentPermissions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services {
   public class ArrangementService : IArrangementService {

      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly IHttpContextAccessor _httpContext;
      private readonly INotifier _notifier;
      private readonly IContentPermissionsService _contentPermissionsService;
      private readonly IHtmlLocalizer<ArrangementService> H;

      public ArrangementService(
         IContentManager contentManager,
         IContentAliasManager aliasManager, 
         IHttpContextAccessor httpContext,
         INotifier notifier,
         IContentPermissionsService contentPermissionsService,
         IHtmlLocalizer<ArrangementService> htmlLocalizer
         ) {
         _aliasManager = aliasManager;
         _contentManager = contentManager;
         _httpContext = httpContext;
         _notifier = notifier;
         _contentPermissionsService = contentPermissionsService;
         H = htmlLocalizer;
      }
      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
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

      public bool IsMissingRequiredParameters(List<Parameter> parameters) {

         var hasRequiredParameters = true;
         foreach (var parameter in parameters.Where(p => p.Required)) {

            var value = _httpContext.HttpContext.Request.Query[parameter.Name].ToString();
            if (value != null && value != "*") {
               continue;
            }

            if (parameter.Sticky && parameter.Value != "*") {
               continue;
            }

            _notifier.Add(NotifyType.Warning, H["{0} is required. To continue, please choose a {0}.", parameter.Label]);
            if (hasRequiredParameters) {
               hasRequiredParameters = false;
            }
         }

         return !hasRequiredParameters;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _contentPermissionsService.CanAccess(contentItem);
      }

   }


}
