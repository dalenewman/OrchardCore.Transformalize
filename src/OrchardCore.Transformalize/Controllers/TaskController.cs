using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.TransformalizeModule.Services.Contracts;
using OrchardCore.TransformalizeModule.ViewModels;
using OrchardCore.TransformalizeModule.Services;
using OrchardCore.TransformalizeModule.Models;

namespace OrchardCore.TransformalizeModule.Controllers {
   public class TaskController : Controller {

      private readonly ITaskService _taskService;
      private readonly CombinedLogger<TaskController> _logger;
      private readonly IFormService _formService;

      public TaskController(
         ITaskService taskService,
         IFormService formService,
         CombinedLogger<TaskController> logger
      ) {
         _taskService = taskService;
         _logger = logger;
         _formService = formService;
      }

      public async Task<ActionResult> Run(string contentItemId, string format = null) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var request = new TransformalizeRequest(contentItemId, user) { Format = format };
         var task = await _taskService.Validate(request);

         if (task.Fails()) {
            return task.ActionResult;
         }

         await _taskService.RunAsync(task.Process);

         if (format == null) {
            return View("Log", new LogViewModel(_logger.Log, task.Process, task.ContentItem));
         } else {
            task.Process.Log.AddRange(_logger.Log);
            task.Process.Connections.Clear();
            return new ContentResult() { Content = task.Process.Serialize(), ContentType = request.ContentType };
         }
      }

      public async Task<ActionResult> Form(string contentItemId) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User.Identity.Name ?? "Anonymous";
         var bulkAction = await _formService.Validate(new TransformalizeRequest(contentItemId, user));

         if (bulkAction.Fails()) {
            return bulkAction.ActionResult;
         }

         return View("Form", bulkAction);
      }

      public async Task<ActionResult> Review(string contentItemId) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User.Identity.Name ?? "Anonymous";

         var task = await _formService.Validate(new TransformalizeRequest(contentItemId, user));

         if (task.Fails()) {
            return task.ActionResult;
         }

         return View(task);
      }

   }
}
