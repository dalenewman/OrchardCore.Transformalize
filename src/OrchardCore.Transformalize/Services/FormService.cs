using OrchardCore.TransformalizeModule.Models;
using OrchardCore.TransformalizeModule.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services {
   public class FormService : IFormService {

      private readonly IArrangementService _arrangementService;
      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementRunService _runService;

      public FormService(
         IArrangementService arrangementService, 
         IArrangementLoadService loadService,
         IArrangementRunService runService
      ) {
         _arrangementService = arrangementService;
         _loadService = loadService;
         _runService = runService;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _arrangementService.CanAccess(contentItem);
      }

      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForForm(ContentItem contentItem, IDictionary<string,string> parameters = null) {
         return _loadService.LoadForForm(contentItem, parameters);
      }

      public async Task RunAsync(Process process) {
         await _runService.RunAsync(process);
      }

      public async Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request) {

         var response = new TransformalizeResponse<TransformalizeTaskPart>(request.Format) {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };

         if (response.ContentItem == null) {
            SetupNotFoundResponse(request, response);
            return response;
         }

         if (request.Secure && !CanAccess(response.ContentItem)) {
            SetupPermissionsResponse(request, response);
            return response;
         }

         response.Process = LoadForForm(response.ContentItem, request.InternalParameters);
         if (response.Process.Status != 200) {
            SetupLoadErrorResponse(request, response);
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
