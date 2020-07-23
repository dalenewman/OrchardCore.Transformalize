using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Activities {

   public class TransformalizeActivity : TaskActivity {

      private readonly IHtmlLocalizer H;
      private readonly IStringLocalizer S;
      private readonly ITaskService _taskService;
      private readonly IWorkflowExpressionEvaluator _expressionEvaluator;

      public TransformalizeActivity(
         ITaskService taskService,
         IStringLocalizer<NotifyTask> s, 
         IHtmlLocalizer<NotifyTask> h,
         IWorkflowExpressionEvaluator expressionEvaluator
      ) {
         H = h;
         S = s;
         _taskService = taskService;
         _expressionEvaluator = expressionEvaluator;
      }

      public override string Name => nameof(TransformalizeActivity);

      public override LocalizedString DisplayText => S["Transformalize"];

      public override LocalizedString Category => S["Transformalize"];

      public WorkflowExpression<string> TaskContentItemId {
         get => GetProperty(() => new WorkflowExpression<string>());
         set => SetProperty(value);
      }

      public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext) {
         return Outcomes(S["Done"], S["Failed"]);
      }

      public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext) {

         var contentItemId = await _expressionEvaluator.EvaluateAsync(TaskContentItemId, workflowContext, null);
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
