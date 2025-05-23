﻿using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;
using System.Linq;

namespace TransformalizeModule.Services {
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

      public async Task<bool> CanAccess(ContentItem contentItem) {
         return await _arrangementService.CanAccess(contentItem);
      }

      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForParameters(ContentItem contentItem, IDictionary<string,string> parameters = null) {
         return _loadService.LoadForParameters(contentItem, parameters);
      }

      public Process LoadForForm(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null) {
         return _loadService.LoadForForm(contentItem, parameters, format);
      }

      public async Task RunAsync(Process process) {
         await _runService.RunAsync(process);
      }

      public void Run(Process process) {
         _runService.Run(process);
      }

      public async Task<TransformalizeResponse<TransformalizeTaskPart>> ValidateParameters(TransformalizeRequest request) {

         var response = new TransformalizeResponse<TransformalizeTaskPart>(request.Format) {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };

         if (response.ContentItem == null) {
            SetupNotFoundResponse(request, response);
            return response;
         }

         var authorized = await CanAccess(response.ContentItem);
         if (request.Secure && !authorized) {
            SetupPermissionsResponse(request, response);
            return response;
         }

         response.Process = LoadForParameters(response.ContentItem, request.InternalParameters);
         if (response.Process.Status != 200) {
            SetupLoadErrorResponse(request, response);
            return response;
         }

         response.Valid = true;
         return response;
      }

      public async Task<TransformalizeResponse<TransformalizeFormPart>> ValidateForm(TransformalizeRequest request) {

         var response = new TransformalizeResponse<TransformalizeFormPart>(request.Format) {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };

         if (response.ContentItem == null) {
            SetupNotFoundResponse(request, response);
            return response;
         }

         var authorized = await CanAccess(response.ContentItem);
         if (request.Secure && !authorized) {
            SetupPermissionsResponse(request, response);
            return response;
         }

         response.Part = response.ContentItem.As<TransformalizeFormPart>();
         if (response.Part == null) {
            SetupWrongTypeResponse(request, response);
            return response;
         }

         response.Process = LoadForForm(response.ContentItem, request.InternalParameters, request.Format);

         if(response.Process.Connections.Where(c=>c.Table != "[default]").Count() != 1) {
            SetupCustomErrorResponse(request, response, "Missing form table in connection.");
            return response;
         }

         if (response.Process.Parameters.Where(p => p.PrimaryKey).Count() != 1) {
            SetupCustomErrorResponse(request, response, "One parameter must be desigated as the primary key.");
            return response;
         }

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

      public void SetupCustomErrorResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response, string error) {
         _arrangementService.SetupCustomErrorResponse(request, response, error);
      }

   }
}
