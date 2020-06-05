using Etch.OrchardCore.ContentPermissions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using OrchardCore.ContentManagement;
using System.Linq;
using System.Threading.Tasks;

namespace Module.Services {
   public class ArrangementService : IArrangementService {

      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly IContentPermissionsService _contentPermissionsService;
      private readonly CombinedLogger<ArrangementService> _logger;

      public ArrangementService(
         IContentManager contentManager,
         IContentAliasManager aliasManager, 
         IContentPermissionsService contentPermissionsService,
         CombinedLogger<ArrangementService> logger
         ) {
         _aliasManager = aliasManager;
         _contentManager = contentManager;
         _contentPermissionsService = contentPermissionsService;
         _logger = logger;
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

      public void SetupInvalidParametersResponse<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         response.Process.Status = 422;
         response.Process.Message = "Parameter Validation Failed";
         if (request.Format == null) {
            foreach (var parameter in response.Process.Parameters.Where(p => !p.Valid)) {
               _logger.Warn(() => parameter.Message);
            }
            response.ActionResult = LogResult(response);
         } else {

            response.Process.Connections.Clear();
            response.Process.Log.AddRange(_logger.Log);
            response.ActionResult = ContentResult(request, response);
         }

      }

      public void SetupPermissionsResponse<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         _logger.Warn(() => $"User {request.User} may not access {response.ContentItem.DisplayText}.");
         if (request.Format == null) {
            response.ActionResult = LogResult(response);
         } else {
            response.Process.Status = 401;
            response.Process.Message = "Unauthorized";
            response.ActionResult = ContentResult(request, response);
         }
      }

      public void SetupNotFoundResponse<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         _logger.Warn(() => $"User {request.User} requested missing content item {request.ContentItemId}.");
         if (request.Format == null) {
            response.ActionResult = LogResult(response);
         } else {
            response.Process.Status = 404;
            response.Process.Message = "Not Found";
            response.ActionResult = ContentResult(request, response);
         }
      }

      public void SetupLoadErrorResponse<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         _logger.Warn(() => $"User {request.User} received error trying to load {response.ContentItem.DisplayText}.");
         if (request.Format == null) {
            response.ActionResult = LogResult(response);
         } else {
            response.Process.Connections.Clear();
            response.Process.Log.AddRange(_logger.Log);
            response.ActionResult = ContentResult(request, response);
         }
      }

      public void SetupWrongTypeResponse<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         _logger.Warn(() => $"User {request.User} requested {response.ContentItem.ContentType} from the report service.");
         if (request.Format == null) {
            response.ActionResult = LogResult(response);
         } else {
            response.Process.Status = 422;
            response.Process.Message = "Invalid Type of Content";
            response.ActionResult = new ContentResult { Content = response.Process.Serialize(), ContentType = request.ContentType };
         }
      }

      public ContentResult ContentResult<T>(TransformalizeRequest request, TransformalizeResponse<T> response) {
         return new ContentResult { Content = response.Process.Serialize(), ContentType = request.ContentType };
      }

      public ViewResult LogResult<T>(TransformalizeResponse<T> response) {
         return View("Log", new LogViewModel(_logger.Log, response.Process, response.ContentItem));
      }

      private ViewResult View(string viewName, object model) {
         return new ViewResult {
            ViewName = viewName,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
               Model = model
            }
         };
      }
   }
}
