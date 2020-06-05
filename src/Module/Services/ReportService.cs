using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace Module.Services {

   public class ReportService : IReportService {

      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementRunService _runService;
      private readonly IArrangementService _arrangementService;
      private readonly IHttpContextAccessor _httpContextAccessor;

      public ReportService(
         IArrangementLoadService loadService,
         IArrangementRunService runService,
         IArrangementService arrangementService,
         IHttpContextAccessor httpContextAccessor
      ) {
         _loadService = loadService;
         _runService = runService;
         _arrangementService = arrangementService;
         _httpContextAccessor = httpContextAccessor;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _arrangementService.CanAccess(contentItem);
      }

      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return await _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForExport(ContentItem contentItem) {
         return _loadService.LoadForExport(contentItem);
      }

      public Process LoadForReport(ContentItem contentItem, string format = null) {
         return _loadService.LoadForReport(contentItem, format);
      }

      public Process LoadForBatch(ContentItem contentItem) {
         return _loadService.LoadForBatch(contentItem);
      }

      public async Task RunAsync(Process process) {
         await _runService.RunAsync(process);
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

         response.Process = LoadForReport(response.ContentItem);
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
