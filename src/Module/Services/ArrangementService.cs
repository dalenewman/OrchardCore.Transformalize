using Etch.OrchardCore.ContentPermissions.Services;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;

namespace Module.Services {
   public class ArrangementService : IArrangementService {

      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly IContentPermissionsService _contentPermissionsService;

      public ArrangementService(
         IContentManager contentManager,
         IContentAliasManager aliasManager, 
         IContentPermissionsService contentPermissionsService
         ) {
         _aliasManager = aliasManager;
         _contentManager = contentManager;
         _contentPermissionsService = contentPermissionsService;
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

      public bool CanAccess(ContentItem contentItem) {
         return _contentPermissionsService.CanAccess(contentItem);
      }

   }


}
