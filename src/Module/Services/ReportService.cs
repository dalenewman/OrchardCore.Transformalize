using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace Module.Services {

   public class ReportService<T> : IReportService<T> {

      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;
      private readonly IArrangementService _arrangementService;
      private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly CombinedLogger<T> _logger;

      public ReportService(
         IArrangementLoadService<T> loadService,
         IArrangementRunService<T> runService,
         IArrangementService arrangementService,
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

      public bool IsMissingRequiredParameters(List<Parameter> parameters) {
         return _arrangementService.IsMissingRequiredParameters(parameters);
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

      public async Task<ReportComponents> Validate(string contentItemId) {
         var result = new ReportComponents {
            ContentItem = await GetByIdOrAliasAsync(contentItemId)
         };
         var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";

         if (result.ContentItem == null) {
            _logger.Warn(() => $"User {user} requested missing content item {contentItemId}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, null, null));
            return result;
         }

         if (!CanAccess(result.ContentItem)) {
            _logger.Warn(() => $"User {user} is may not access {result.ContentItem.DisplayText}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, null, null));
            return result;
         }

         result.Part = result.ContentItem.As<TransformalizeReportPart>();
         if (result.Part == null) {
            _logger.Warn(() => $"User {user} requested incompatible content type for {result.ContentItem.DisplayText}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, null, null));
            return result;
         }

         result.Process = LoadForReport(result.ContentItem, _logger);
         if (result.Process.Status != 200) {
            _logger.Warn(() => $"User {user} received error trying to load report {result.ContentItem.DisplayText}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, result.Process, result.ContentItem));
            return result;
         }

         if (IsMissingRequiredParameters(result.Process.Parameters)) {
            _logger.Error(() => $"User {user} is trying to run report {result.ContentItem.DisplayText} without required parameters.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, result.Process, result.ContentItem));
            return result;
         }

         result.Valid = true;
         return result;
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
