using Module.Models;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface IArrangementService<TLog> {
      Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias);
      bool CanAccess(ContentItem contentItem);
      void SetupInvalidParametersResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response);
      void SetupPermissionsResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response);
      void SetupNotFoundResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response);
      void SetupLoadErrorResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response);
      void SetupWrongTypeResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response);
   }
}
