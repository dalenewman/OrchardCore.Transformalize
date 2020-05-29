using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Logging;
using Transformalize.Context;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using System.Collections.Generic;
using Module.Services;

namespace Module.Controllers {
   public class TaskController : Controller {

      private readonly ITaskService<TaskController> _taskService;
      private readonly CombinedLogger<TaskController> _logger;

      public TaskController(
         ITaskService<TaskController> taskService,
         CombinedLogger<TaskController> logger
      ) {
         _taskService = taskService;
         _logger = logger;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await _taskService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_taskService.CanAccess(contentItem)) {
            return View(GetErrorModel(contentItem, "Access Denied."));
         }

         var process = _taskService.LoadForTask(contentItem, _logger);

         if (process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, process, contentItem));
         }

         if (_taskService.IsMissingRequiredParameters(process.Parameters)) {
            return View("Log", new LogViewModel(_logger.Log, process, contentItem));
         }

         await _taskService.RunAsync(process, _logger);
         if (process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, process, contentItem));
         }

         return View("Log", new LogViewModel(_logger.Log, process, contentItem));

      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         // important: since you're exposing the configuration, 
         // always clear out the connections before sending to the client

         var contentItem = await _taskService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var contentType = format == "json" ? "application/json" : "application/xml";

         if (!_taskService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _taskService.LoadForTask(contentItem, _logger, null, format);

         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         var context = new PipelineContext(_logger, process);

         if (_taskService.IsMissingRequiredParameters(process.Parameters)) {
            context.Error("Missing required parameter(s)");
            process.Message = "Error";
            process.Status = 500;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         await _taskService.RunAsync(process, _logger);
         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         process.Connections.Clear();
         return new ContentResult() { Content = process.Serialize(), ContentType = contentType };

      }

      public LogViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new LogViewModel(_logger.Log, new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(Transformalize.Contracts.LogLevel.Error, null, message) } }, contentItem);
      }

   }
}
