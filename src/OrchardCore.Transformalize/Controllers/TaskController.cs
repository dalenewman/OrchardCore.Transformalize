using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Models;
using TransformalizeModule.Ext;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Controllers {

   [Authorize]
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

         var request = new TransformalizeRequest(contentItemId) { 
            Format = format, 
            InternalParameters = Common.GetFileParameters(Request)
         };
         var task = await _taskService.Validate(request);

         if (task.Fails()) {
            return task.ActionResult;
         }

         await _taskService.RunAsync(task.Process, HttpContext.RequestAborted);

         if (format == null) {
            return View("Log", new LogViewModel(_logger.Log, task.Process, task.ContentItem));
         } else {
            task.Process.Log.AddRange(_logger.Log);
            task.Process.Connections.Clear();
            return new ContentResult() { Content = task.Process.Serialize(), ContentType = request.ContentType };
         }
      }

      public async Task<ActionResult> Form(string contentItemId) {

         var bulkAction = await _formService.ValidateParameters(new TransformalizeRequest(contentItemId));

         if (bulkAction.Fails()) {
            return this.FormFailure(bulkAction.Process, bulkAction.ActionResult);
         }

         return this.FormFragment(Interactivity(bulkAction.Process, contentItemId));
      }

      public async Task<ActionResult> Review(string contentItemId) {

         var task = await _formService.ValidateParameters(new TransformalizeRequest(contentItemId));

         if (task.Fails()) {
            return task.ActionResult;
         }

         this.SetFormInteractivity(Interactivity(task.Process, contentItemId));
         return View(task);
      }

      private FormInteractivityViewModel Interactivity(Transformalize.Configuration.Process process, string contentItemId) => new() {
         Process = process,
         ValidationUrl = Url.Action("Form", "Task", new { Area = Common.ModuleName, ContentItemId = contentItemId, modal = Request.Query["modal"].ToString() })!,
         SubmitUrl = Url.Action("Review", "Task", new { Area = Common.ModuleName, ContentItemId = contentItemId, modal = Request.Query["modal"].ToString() })!,
         RunUrl = Url.Action("Run", "Task", new { Area = Common.ModuleName, ContentItemId = contentItemId, modal = Request.Query["modal"].ToString() })!,
      };

   }
}
