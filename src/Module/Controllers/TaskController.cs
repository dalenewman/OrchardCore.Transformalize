using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Module.Services;
using Module.Models;

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

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var task = await _taskService.Validate(new TransformalizeRequest(contentItemId, user));

         if (task.Fails()) {
            return task.ActionResult;
         }

         await _taskService.RunAsync(task.Process, _logger);
         if (task.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, task.Process, task.ContentItem));
         }

         return View("Log", new LogViewModel(_logger.Log, task.Process, task.ContentItem));

      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var request = new TransformalizeRequest(contentItemId, user) { Format = format };
         var task = await _taskService.Validate(request);

         if (task.Fails()) {
            return task.ActionResult;
         }

         await _taskService.RunAsync(task.Process, _logger);

         task.Process.Connections.Clear();
        
         return new ContentResult() { Content = task.Process.Serialize(), ContentType = request.ContentType };

      }

   }
}
