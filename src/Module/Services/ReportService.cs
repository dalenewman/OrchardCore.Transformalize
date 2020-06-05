using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace Module.Services {

   public class ReportService<T> : IReportService<T> {

      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;
      private readonly IArrangementService<T> _arrangementService;
      private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly CombinedLogger<T> _logger;

      public ReportService(
         IArrangementLoadService<T> loadService,
         IArrangementRunService<T> runService,
         IArrangementService<T> arrangementService,
         IHttpContextAccessor httpContextAccessor,
         CombinedLogger<T> logger
      ) {
         _loadService = loadService;
         _runService = runService;
         _arrangementService = arrangementService;
         _httpContextAccessor = httpContextAccessor;
         _logger = logger;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _arrangementService.CanAccess(contentItem);
      }

      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return await _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForExport(ContentItem contentItem, CombinedLogger<T> logger) {
         return _loadService.LoadForExport(contentItem, logger);
      }

      public Process LoadForReport(ContentItem contentItem, CombinedLogger<T> logger, string format = null) {
         return _loadService.LoadForReport(contentItem, logger, format);
      }

      public Process LoadForBatch(ContentItem contentItem, CombinedLogger<T> logger) {
         return _loadService.LoadForBatch(contentItem, logger);
      }

      public async Task RunAsync(Process process, CombinedLogger<T> logger) {
         await _runService.RunAsync(process, logger);
      }

      public async Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request) {
         
         var response = new TransformalizeResponse<TransformalizeReportPart>(request.Format) {
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

         response.Part = response.ContentItem.As<TransformalizeReportPart>();
         if (response.Part == null) {
            SetupWrongTypeResponse(request, response);
            return response;
         }

         response.Process = LoadForReport(response.ContentItem, _logger);
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
