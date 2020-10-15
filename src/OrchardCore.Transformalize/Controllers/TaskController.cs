using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;
using TransformalizeModule.Services;
using TransformalizeModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class TaskController : Controller {

      private readonly ITaskService _taskService;
      private readonly CombinedLogger<TaskController> _logger;
      private readonly IFormService _formService;
      private readonly IHttpContextAccessor _httpContext;

      public TaskController(
         ITaskService taskService,
         IFormService formService,
         IHttpContextAccessor httpContext,
         CombinedLogger<TaskController> logger
      ) {
         _taskService = taskService;
         _logger = logger;
         _formService = formService;
         _httpContext = httpContext;
      }

      public async Task<ActionResult> Run(string contentItemId, string format = null) {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { 
            Format = format, 
            InternalParameters = GetFileParameters()
         };
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

         var bulkAction = await _formService.ValidateParameters(new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name));

         if (bulkAction.Fails()) {
            return bulkAction.ActionResult;
         }

         return View("Form", bulkAction.Process);
      }

      public async Task<ActionResult> Review(string contentItemId) {

         var task = await _formService.ValidateParameters(new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name));

         if (task.Fails()) {
            return task.ActionResult;
         }

         return View(task);
      }

      /// <summary>
      /// Gets the file parameter content item ids that are posted in as {name}_Old
      /// </summary>
      /// <returns>returns a dictionary of parameters to be passed in as "internal parameters"</returns>
      private Dictionary<string, string> GetFileParameters() {
         var parameters = new Dictionary<string, string>();
         if (Request.Method == "POST" && _httpContext.HttpContext.Request.HasFormContentType) {
            // the content item id of the last stored file is in the _Old parameter
            foreach (var parameter in _httpContext.HttpContext.Request.Form) {
               if (parameter.Key.EndsWith("_Old")) {
                  var name = parameter.Key.Substring(0, parameter.Key.Length - 4);
                  parameters[name] = parameter.Value.ToString();
               }
            }
         }
         return parameters;
      }

   }
}
