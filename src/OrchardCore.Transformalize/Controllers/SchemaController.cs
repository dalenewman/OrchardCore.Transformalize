using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class SchemaController : Controller {

      private readonly ISchemaService _schemaService;
      private readonly CombinedLogger<TaskController> _logger;

      public SchemaController(
         ISchemaService taskService,
         CombinedLogger<TaskController> logger
      ) {
         _schemaService = taskService;
         _logger = logger;
      }

      public async Task<ActionResult> Index(string contentItemId, string format = "xml") {

         var request = new TransformalizeRequest(contentItemId) { Format = format };
         var task = await _schemaService.Validate(request);

         if (task.Fails()) {
            return task.ActionResult;
         }

         var process = await _schemaService.GetSchemaAsync(task.Process);

         if (format == null) {
            return View("Log", new LogViewModel(_logger.Log, process, task.ContentItem));
         } else {
            task.Process.Log.AddRange(_logger.Log);
            task.Process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = request.ContentType };
         }
      }

   }
}
