using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Activities {

   public class TransformalizeTask : TaskActivity {

      private readonly IHtmlLocalizer H;
      private readonly IStringLocalizer S;
      private readonly ITaskService _taskService;

      public TransformalizeTask(
         ITaskService taskService,
         IStringLocalizer<NotifyTask> s, 
         IHtmlLocalizer<NotifyTask> h
      ) {
         H = h;
         S = s;
         _taskService = taskService;
      }

      public override string Name => nameof(TransformalizeTask);

      public override LocalizedString DisplayText => S["Transformalize Task"];

      public override LocalizedString Category => S["Transformalize"];

      public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext) {
         return Outcomes(S["Done"], S["Failed"]);
      }

      public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext) {
         
         var contentItemId = workflowContext.Properties["ContentItemId"].ToString();
         var request = new TransformalizeRequest(contentItemId, "Workflow") { Secure = false };
         var task = await _taskService.Validate(request);

         if (task.Fails()) {
            workflowContext.Fault(new Exception(task.Process.Message), activityContext);
            return Outcomes("Failed");
         }

         await _taskService.RunAsync(task.Process);

         if(task.Process.Status != 200) {
            workflowContext.Fault(new Exception(task.Process.Message), activityContext);
            return Outcomes("Failed");
         }

         return Outcomes("Done");
      }

   }
}
