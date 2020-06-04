using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services {
   public class TaskService<T> : ITaskService<T> {

      private readonly IArrangementService _arrangementService;
      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;
      private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly CombinedLogger<T> _logger;

      public TaskService(
         IArrangementService arrangementService, 
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

      public async Task<TaskComponents> Validate(ValidateRequest request) {

         var result = new TaskComponents {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };
         var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";

         if (result.ContentItem == null) {
            _logger.Warn(() => $"User {user} requested missing content item {request.ContentItemId}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, null, null));
            return result;
         }

         if (request.Secure && !CanAccess(result.ContentItem)) {
            _logger.Warn(() => $"User {user} is may not access {result.ContentItem.DisplayText}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, null, null));
            return result;
         }

         result.Process = LoadForTask(result.ContentItem, _logger, request.InternalParameters);
         if (result.Process.Status != 200) {
            _logger.Warn(() => $"User {user} received error trying to load task {result.ContentItem.DisplayText}.");
            result.ActionResult = View("Log", new LogViewModel(_logger.Log, result.Process, result.ContentItem));
            return result;
         }

         if (!result.Process.Parameters.All(p => p.Valid)) {
            foreach (var parameter in result.Process.Parameters.Where(p => !p.Valid)) {
               _logger.Warn(() => parameter.Message);
            }
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
