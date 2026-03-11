using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using System.Dynamic;
using System.Text;
using Transformalize.Configuration;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class BulkActionController : Controller {

      private readonly ITaskService _taskService;
      private readonly IReportService _reportService;
      private readonly IFormService _formService;
      private readonly CombinedLogger<BulkActionController> _logger;
      private readonly IUserService _userService;
      private readonly ISettingsService _settingsService;

      public BulkActionController(
         ITaskService taskService,
         IReportService reportService,
         IFormService formService,
         ISettingsService settingsService,
         IUserService userService,
         CombinedLogger<BulkActionController> logger
      ) {
         _taskService = taskService;
         _reportService = reportService;
         _formService = formService;
         _userService = userService;
         _settingsService = settingsService;
         _logger = logger;
      }

      /// <summary>
      /// 1. a user viewing a report has requested a bulk action<br/>
      /// 2. generate batch and record report, task, and user details using batch create task<br/>
      /// 3. record selected or all report records using batch write task<br/>
      /// 4. direct user to review action
      /// </summary>
      [HttpPost]
      public async Task<ActionResult> Create(BulkActionRequest request) {

         var report = await _reportService.Validate(new TransformalizeRequest(request.ContentItemId));
         if (report.Fails()) {
            return report.ActionResult;
         }

         var referrer = Request.Form.ContainsKey(Common.ReturnUrlName) ? Request.Form[Common.ReturnUrlName].ToString() : Url.Action("Index", "Report", new { request.ContentItemId });

         // confirm we have an action registered in the report
         if (report.Process.Actions.Any(a => a.Name == request.ActionName)) {

            var bulkAction = await _taskService.Validate(new TransformalizeRequest(request.ActionName) { Secure = false });

            if (bulkAction.Fails() && bulkAction.Process.Message != Common.InvalidParametersMessage) {
               return bulkAction.ActionResult;
            }

            var taskNames = _settingsService.GetBulkActionTaskNames(report.Part);

            #region batch creation
            var user = await _userService.GetUserAsync(HttpContext.User.Identity.Name) as User;

            var createParameters = new Dictionary<string, string> {

               { Common.TaskReferrer, referrer },
               { Common.TaskContentItemId, bulkAction.ContentItem.ContentItemId },
               { Common.ReportContentItemId, report.ContentItem.ContentItemId },

               { "UserId", user.Id.ToString() },
               { "UserName", user.UserName },
               { "UserEmail", user.Email },

               { "ReportId", report.ContentItem.Id.ToString() },
               { "ReportContentItemVersionId", report.ContentItem.ContentItemVersionId },
               { "ReportTitle", report.ContentItem.DisplayText },

               { "TaskId", bulkAction.ContentItem.ContentItemId },
               { "TaskContentItemVersionId", bulkAction.ContentItem.ContentItemVersionId },
               { "TaskTitle", bulkAction.ContentItem.DisplayText },


               { "Description", report.Process.Actions.First(a=>a.Name == request.ActionName).Description }
            };

            var create = await _taskService.Validate(new TransformalizeRequest(taskNames.Create) { Secure = false, InternalParameters = createParameters });

            if (create.Fails()) {
               return create.ActionResult;
            }

            await _taskService.RunAsync(create.Process);
            if (create.Process.Status != 200) {
               _logger.Warn(() => $"User {user.UserName} received error running action {taskNames.Create}.");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }

            var entity = create.Process.Entities.FirstOrDefault();

            if (entity == null) {
               _logger.Error(() => $"The {taskNames.Create} task is missing an entity.  It needs an entity with at least one row that returns one row of data (e.g. the BatchId).");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }

            if (!entity.Rows.Any()) {
               _logger.Error(() => $"The {taskNames.Create} task didn't produce a row (e.g. a single row with a BatchId need to associate the batch values with this batch).");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }
            #endregion

            #region batch writing
            var writeParameters = new Dictionary<string, string> {
               { Common.TaskReferrer, referrer },
               { Common.TaskContentItemId, bulkAction.ContentItem.ContentItemId },
               { Common.ReportContentItemId, report.ContentItem.ContentItemId } 
            };

            foreach (var field in entity.GetAllOutputFields()) {
               writeParameters[field.Alias] = entity.Rows[0][field.Alias].ToString();
            }

            var write = await _taskService.Validate(new TransformalizeRequest(taskNames.Write) { Secure = false, InternalParameters = writeParameters });

            if (write.Fails()) {
               return write.ActionResult;
            }

            // potential memory problem (could be solved by merging report and batch write into one process)
            var writeEntity = write.Process.Entities.First();
            var batchValueField = writeEntity.Fields.LastOrDefault(f => f.Input && f.Output);

            if (batchValueField == null) {
               _logger.Error(() => $"Could not identify batch value field in {taskNames.Write}.");
               return View("Log", new LogViewModel(_logger.Log, write.Process, write.ContentItem));
            }

            if (request.ActionCount == 0) {
               var batchProcess = _reportService.LoadForBatch(report.ContentItem);

               await _taskService.RunAsync(batchProcess);
               foreach (var batchRow in batchProcess.Entities.First().Rows) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchRow[report.Part.BulkActionValueField.Text];
                  writeEntity.Rows.Add(row);
               }
            } else {
               foreach (var batchValue in request.Records) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchValue;
                  writeEntity.Rows.Add(row);
               }
            }

            await _taskService.RunAsync(write.Process);
            #endregion

            return RedirectToAction("Review", ParametersToRouteValues(writeParameters));

         } else {
            _logger.Warn(() => $"User {HttpContext.User.Identity.Name} called missing action {request.ActionName} in {report.ContentItem.DisplayText}.");
         }

         return View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem));

      }

      /// <summary>
      /// 1. check the report definition to determine it's bulk action tasks<br/>
      /// 2. get a batch summary with the batch summary task<br/>
      /// 3. validate the task's parameters<br/>
      /// 4. display review<br/>
      /// </summary>
      public async Task<ActionResult> Review(BulkActionReviewRequest request) {

         var report = await _reportService.Validate(new TransformalizeRequest(request.ReportContentItemId));
         if (report.Fails()) {
            return report.ActionResult;
         }
         var taskNames = _settingsService.GetBulkActionTaskNames(report.Part);

         var batchSummary = await _taskService.Validate(new TransformalizeRequest(taskNames.Summary) { Secure = false });
         if (batchSummary.Fails()) {
            return batchSummary.ActionResult;
         }
         await _taskService.RunAsync(batchSummary.Process);

         var bulkAction = await _formService.ValidateParameters(new TransformalizeRequest(request.TaskContentItemId));
         if (bulkAction.Fails()) {
            return bulkAction.ActionResult;
         }

         return View(new BulkActionViewModel(TransferRequiredParameters(request, bulkAction), batchSummary.Process));
      }

      public async Task<ActionResult> Form(BulkActionReviewRequest request) {

         var bulkAction = await _formService.ValidateParameters(new TransformalizeRequest(request.TaskContentItemId));
         if (bulkAction.Fails()) {
            return bulkAction.ActionResult;
         }

         return View("Form", TransferRequiredParameters(request, bulkAction).Process);
      }

      /// <summary>
      /// 1. check the report definition to determine it's bulk action tasks<br/>
      /// 2. check and run the bulk action (aka task)<br/>
      /// 3. if bulk action succeeds, run batch success task<br/>
      /// 4. if bulk action fails, run batch fail task<br/>
      /// 9. direct user to result action<br/>
      /// </summary>
      public async Task<ActionResult> Run(BulkActionReviewRequest request) {

         var report = await _reportService.Validate(new TransformalizeRequest(request.ReportContentItemId));
         if (report.Fails()) {
            return report.ActionResult;
         }
         var taskNames = _settingsService.GetBulkActionTaskNames(report.Part);

         var bulkAction = await _taskService.Validate(new TransformalizeRequest(request.TaskContentItemId));
         if (bulkAction.Fails()) {
            return bulkAction.ActionResult;
         }
         await _taskService.RunAsync(bulkAction.Process);

         var records = CountAffectedRecords(bulkAction.Process);
         var closeParameters = new Dictionary<string, string>() { { "RecordsAffected", records.ToString() } };

         if (bulkAction.Process.Status == 200) {
            var batchSuccess = await _taskService.Validate(new TransformalizeRequest(taskNames.Success) { InternalParameters = closeParameters });

            if (batchSuccess.Fails()) {
               _logger.Warn(() => $"{bulkAction.ContentItem.DisplayText} succeeded but {taskNames.Success} failed to load.");
            } else {
               await _taskService.RunAsync(batchSuccess.Process);
            }
         } else {
            var message = new StringBuilder(bulkAction.Process.Message);
            foreach (var error in _logger.Log.Where(l => l.LogLevel == Transformalize.Contracts.LogLevel.Error)) {
               message.AppendLine(error.Message);
            }
            closeParameters["Message"] = message.ToString();
            var batchFail = await _taskService.Validate(new TransformalizeRequest(taskNames.Fail) { InternalParameters = closeParameters });

            if (batchFail.Fails()) {
               _logger.Warn(() => $"{bulkAction.ContentItem.DisplayText} failed and {taskNames.Fail} failed to load.");
            } else {
               await _taskService.RunAsync(batchFail.Process);
            }
         }

         TransferRequiredParameters(request, bulkAction);

         return RedirectToAction("Result", ParametersToRouteValues(bulkAction.Process.Parameters.Where(p => p.Input)));
      }

      /// <summary>
      /// 1. check the report definition to determine it's bulk action tasks<br/>
      /// 2. get a batch summary with the batch summary task<br/>
      /// 3. display results
      /// </summary>
      public async Task<ActionResult> Result(BulkActionReviewRequest request) {

         var report = await _reportService.Validate(new TransformalizeRequest(request.ReportContentItemId));
         if (report.Fails()) {
            return report.ActionResult;
         }
         var taskNames = _settingsService.GetBulkActionTaskNames(report.Part);

         var batchSummary = await _taskService.Validate(new TransformalizeRequest(taskNames.Summary) { Secure = false });
         if (batchSummary.Fails()) {
            return batchSummary.ActionResult;
         }
         await _taskService.RunAsync(batchSummary.Process);

         return View(TransferRequiredParameters(request, batchSummary).Process);
      }

      internal static TransformalizeResponse<TransformalizeTaskPart> TransferRequiredParameters(BulkActionReviewRequest request, TransformalizeResponse<TransformalizeTaskPart> response) {

         var existing = new HashSet<string>(response.Process.Parameters.Select(p => p.Name));

         if (!existing.Contains(Common.TaskReferrer)) {
            response.Process.Parameters.Add(new Parameter { Name = Common.TaskReferrer, Value = request.TaskReferrer });
         }

         if (!existing.Contains(Common.TaskContentItemId)) {
            response.Process.Parameters.Add(new Parameter { Name = Common.TaskContentItemId, Value = request.TaskContentItemId });
         }

         if (!existing.Contains(Common.ReportContentItemId)) {
            response.Process.Parameters.Add(new Parameter { Name = Common.ReportContentItemId, Value = request.ReportContentItemId });
         }

         return response;
      }

      private bool IsModalRequest() => Request.Query["modal"] == "1" || Request.Form["modal"] == "1";

      private dynamic ParametersToRouteValues(IDictionary<string, string> parameters) =>
         ParametersToRouteValues(parameters, IsModalRequest());

      private dynamic ParametersToRouteValues(IEnumerable<Parameter> parameters) =>
         ParametersToRouteValues(parameters, IsModalRequest());

      internal static dynamic ParametersToRouteValues(IDictionary<string, string> parameters, bool modal) {
         var routeValues = new ExpandoObject();
         var editable = (IDictionary<string, object?>)routeValues;
         foreach (var kvp in parameters) {
            editable[kvp.Key] = kvp.Value;
         }
         if (modal) {
            editable["modal"] = "1";
         }
         return (dynamic)routeValues;
      }

      internal static dynamic ParametersToRouteValues(IEnumerable<Parameter> parameters, bool modal) {
         var routeValues = new ExpandoObject();
         var editable = (IDictionary<string, object?>)routeValues;
         foreach (var p in parameters) {
            editable[p.Name] = p.Value;
         }
         if (modal) {
            editable["modal"] = "1";
         }
         return (dynamic)routeValues;
      }

      internal static string GetFieldFromSummary(Process process, string fieldName) {
         if (process != null) {
            if (process.Entities.Any() && process.Entities[0].Rows.Any()) {
               var fields = process.Entities[0].GetAllOutputFields();
               var field = fields.FirstOrDefault(f => f.Alias == fieldName);
               if (field != null) {
                  return process.Entities[0].Rows[0][fieldName]?.ToString() ?? string.Empty;
               }
            }
         }
         return null;
      }

      internal static int CountAffectedRecords(Process process) {
         return process.Actions.Where(a => a.RowCount > 0).Sum(a => a.RowCount)
              + process.Entities.Where(e => e.Hits > 0).Sum(e => e.Hits)
              + process.Entities.SelectMany(e => e.Rows).Count();
      }

   }
}
