﻿using GraphQL;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace TransformalizeModule.Services {
   public class SchemaService : ISchemaService {

      private readonly IArrangementService _arrangementService;
      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementSchemaService _schemaService;

      public SchemaService(
         IArrangementService arrangementService, 
         IArrangementLoadService loadService,
         IArrangementSchemaService schemaService
      ) {
         _arrangementService = arrangementService;
         _loadService = loadService;
         _schemaService = schemaService;
      }

      public async Task<bool> CanAccess(ContentItem contentItem) {
         return await _arrangementService.CanAccess(contentItem);
      }

      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForSchema(ContentItem contentItem, string format) {
         return _loadService.LoadForSchema(contentItem, format);
      }

      public async Task<Process> GetSchemaAsync(Process process) {
         return await _schemaService.GetSchemaAsync(process);
      }

      public async Task<TransformalizeResponse<ContentPart>> Validate(TransformalizeRequest request) {

         var response = new TransformalizeResponse<ContentPart>(request.Format) {
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

         response.Process = LoadForSchema(response.ContentItem, request.Format);
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
