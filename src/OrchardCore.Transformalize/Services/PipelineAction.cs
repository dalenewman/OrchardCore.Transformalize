using Microsoft.Extensions.DependencyInjection;
using TransformalizeModule.Services.Contracts;
using Transformalize;
using Transformalize.Contracts;
using Action = Transformalize.Configuration.Action;

namespace TransformalizeModule.Services {
   public class PipelineAction : IAction {

      private readonly IServiceProvider _serviceProvider;
      private readonly Action _action;

      public PipelineAction(
         Action action, 
         IServiceProvider serviceProvider
      ) {
         _action = action;
         _serviceProvider = serviceProvider;
      }

      public ActionResponse Execute() {
         throw new NotImplementedException("Please use the async version of this method.");
      }

      public async Task<ActionResponse> ExecuteAsync() {
         var response = new ActionResponse() { Action = _action };

         var taskService = _serviceProvider.GetRequiredService<ITaskService>();

         if (!string.IsNullOrEmpty(_action.Name)) {
            var contentItem = taskService.GetByIdOrAliasAsync(_action.Name);
            if (contentItem.Result != null) {
               var process = await taskService.LoadForTaskAsync(contentItem.Result);
               await taskService.RunAsync(process);
               response.Code = process.Status;
               response.Message = process.Message;
            } else {
               response.Code = 404;
               response.Message = $"Could not find content item {_action.Name}.";
            }
         } else {
            response.Code = 500;
            response.Message = "Please specify tfl action name.  The name is the alias or content item id.";
         }

         return response;
      }

      public async Task<ActionResponse> ExecuteAsync(CancellationToken token = default) {
         var response = new ActionResponse() { Action = _action };

         var taskService = _serviceProvider.GetRequiredService<ITaskService>();

         if (!string.IsNullOrEmpty(_action.Name)) {
            var contentItem = taskService.GetByIdOrAliasAsync(_action.Name);
            if (contentItem.Result != null) {
               var process = await taskService.LoadForTaskAsync(contentItem.Result);
               await taskService.RunAsync(process);
               response.Code = process.Status;
               response.Message = process.Message;
            } else {
               response.Code = 404;
               response.Message = $"Could not find content item {_action.Name}.";
            }
         } else {
            response.Code = 500;
            response.Message = "Please specify tfl action name.  The name is the alias or content item id.";
         }

         return response;
      }

   }
}
