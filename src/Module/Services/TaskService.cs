using Microsoft.AspNetCore.Http;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services {
   public class TaskService<T> : ITaskService<T> {

      private readonly IArrangementService<T> _arrangementService;
      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;
      private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly CombinedLogger<T> _logger;

      public TaskService(
         IArrangementService<T> arrangementService, 
         IArrangementLoadService<T> loadService,
         IHttpContextAccessor httpContextAccessor,
         IArrangementRunService<T> runService,
         CombinedLogger<T> logger
      ) {
         _arrangementService = arrangementService;
         _loadService = loadService;
         _runService = runService;
         _httpContextAccessor = httpContextAccessor;
         _logger = logger;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _arrangementService.CanAccess(contentItem);
      }

      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForTask(ContentItem contentItem, CombinedLogger<T> logger, IDictionary<string,string> parameters = null, string format = null) {
         return _loadService.LoadForTask(contentItem, logger, parameters, format);
      }

      public async Task RunAsync(Process process, CombinedLogger<T> logger) {
         await _runService.RunAsync(process, logger);
      }

      public async Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request) {

         var response = new TransformalizeResponse<TransformalizeTaskPart>(request.Format) {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };
         var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";

         if (response.ContentItem == null) {
            SetupNotFoundResponse(request, response);
            return response;
         }

         if (request.Secure && !CanAccess(response.ContentItem)) {
            SetupPermissionsResponse(request, response);
            return response;
         }

         response.Process = LoadForTask(response.ContentItem, _logger, request.InternalParameters, request.Format);
         if (response.Process.Status != 200) {
            SetupLoadErrorResponse(request, response);
            return response;
         }

         if (!response.Process.Parameters.All(p => p.Valid)) {
            SetupInvalidParametersResponse(request, response);
            return response;
         }

         response.Valid = true;
         return response;
      }

      public void SetupInvalidParametersResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupInvalidParametersResponse(request, response);
      }

      public void SetupPermissionsResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupPermissionsResponse(request, response);
      }

      public void SetupNotFoundResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupNotFoundResponse(request, response);
      }

      public void SetupLoadErrorResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupLoadErrorResponse(request, response);
      }

      public void SetupWrongTypeResponse<T1>(TransformalizeRequest request, TransformalizeResponse<T1> response) {
         _arrangementService.SetupWrongTypeResponse(request, response);
      }
   }
}
