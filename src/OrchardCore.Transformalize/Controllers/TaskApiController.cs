using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Transformalize.Configuration;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Controllers {

   [Route("t/api/task")]
   [ApiController]
   [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
   public class TaskApiController : Controller {

      private readonly ITaskService _taskService;
      private readonly CombinedLogger<TaskController> _logger;
      private readonly IAuthorizationService _authorizationService;

      public TaskApiController(
         ITaskService taskService,
         CombinedLogger<TaskController> logger,
         IAuthorizationService authorizationService
      ) {
         _taskService = taskService;
         _logger = logger;
         _authorizationService = authorizationService;
      }

      [HttpPost, HttpGet]
      [Route("run/{contentItemId}")]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         if (!await _authorizationService.AuthorizeAsync(User, Permissions.AllowApi)) {
            var process = format == "json" ? new Process("{ \"name\":\"401\" }") : new Process("<cfg name=\"401\" />");
            process.Status = 401;
            process.Message = "Unauthorized";
            process.Connections.Clear();
            return new ContentResult { 
               Content = process.Serialize(),
               ContentType = format == "json" ? "application/json" : "application/xml" 
            };
         }

         var request = new TransformalizeRequest(contentItemId) {
            Format = format,
            InternalParameters = Common.GetFileParameters(Request)
         };

         var task = await _taskService.Validate(request);

         if (task.Fails()) {
            return task.ActionResult;
         }

         await _taskService.RunAsync(task.Process);

         task.Process.Log.AddRange(_logger.Log);
         task.Process.Connections.Clear();

         return new ContentResult { 
            Content = task.Process.Serialize(), 
            ContentType = request.ContentType 
         };
      }

   }
}
