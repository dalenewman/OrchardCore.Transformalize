using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Module.Services;
using Module.Models;

namespace Module.Controllers {
   public class SchemaController : Controller {

      private readonly ITaskService _taskService;
      private readonly CombinedLogger<TaskController> _logger;
      private readonly IArrangementSchemaService _schemaService;

      public SchemaController(
         ITaskService taskService,
         IArrangementSchemaService schemaService,
         CombinedLogger<TaskController> logger
      ) {
         _taskService = taskService;
         _logger = logger;
         _schemaService = schemaService;
      }

      public async Task<ActionResult> Index(string contentItemId, string format = "xml") {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         // invalid parameters removes the connections when sending data back (xml, json) and that would not allow us to get the schema so we do not validate parameters here
         var request = new TransformalizeRequest(contentItemId, user) { Format = format, ValidateParameters = false };
         var task = await _taskService.Validate(request);

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
