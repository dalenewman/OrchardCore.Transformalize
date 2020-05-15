using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Context;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using System.Collections.Generic;

namespace Module.Controllers {
   public class TaskController : Controller {

      private readonly ITaskService _taskService;

      public TaskController(
         ITaskService taskService
      ) {
         _taskService = taskService;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await _taskService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_taskService.CanAccess(contentItem)) {
            return View(GetErrorModel(contentItem, "Access Denied."));
         }

         var process = _taskService.LoadForTask(contentItem, logger);

         if (process.Status != 200) {
            return View(new TaskViewModel(process, contentItem));
         }

         if (_taskService.IsMissingRequiredParameters(process.Parameters)) {
            return View(new TaskViewModel(process, contentItem));
         }

         await _taskService.RunAsync(process, logger);
         if (process.Status != 200) {
            return View(new TaskViewModel(process, contentItem));
         }

         process.Log = logger.Log;
         return View(new TaskViewModel(process, contentItem));

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

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_taskService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _taskService.LoadForTask(contentItem, logger, format);

         if (process.Status != 200) {
            process.Log = logger.Log;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         var context = new PipelineContext(logger, process);

         if (_taskService.IsMissingRequiredParameters(process.Parameters)) {
            context.Error("Missing required parameter(s)");
            process.Log = logger.Log;
            process.Message = "Error";
            process.Status = 500;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         await _taskService.RunAsync(process, logger);
         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         process.Connections.Clear();
         return new ContentResult() { Content = process.Serialize(), ContentType = contentType };

      }

      public TaskViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new TaskViewModel(new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(LogLevel.Error, null, message) } }, contentItem);
      }

   }
}
