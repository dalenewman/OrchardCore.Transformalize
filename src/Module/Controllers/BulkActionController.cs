using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using System.Collections.Generic;
using Module.Models;
using System.Linq;
using Module.Services;
using System.Dynamic;
using Transformalize.Configuration;

namespace Module.Controllers {

   public class BulkActionController : Controller {

      private readonly ITaskService<BulkActionController> _taskService;
      private readonly IReportService<BulkActionController> _reportService;
      private readonly CombinedLogger<BulkActionController> _logger;

      public BulkActionController(
         ITaskService<BulkActionController> taskService,
         IReportService<BulkActionController> reportService,
         CombinedLogger<BulkActionController> logger
      ) {
         _taskService = taskService;
         _reportService = reportService;
         _logger = logger;
      }

      [HttpPost]
      public async Task<ActionResult> Index(BulkActionRequest bar) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User.Identity.Name ?? "Anonymous";

         var report = await _reportService.Validate(bar.ContentItemId);
         if (report.Fails()) {
            return report.ActionResult;
         }

         var referrer = Request.Headers.ContainsKey("Referer") ? Request.Headers["Referer"].ToString() : Url.Action("Index", "Report", new { bar.ContentItemId });

         // confirm we have an action registered in the report
         if (report.Process.Actions.Any(a => a.Name == bar.ActionName)) {

            #region the bulk action in question (not doing anything yet)
            var bulkAction = await _taskService.GetByIdOrAliasAsync(bar.ActionName);

            if (bulkAction == null) {
               _logger.Warn(() => $"User {user} requested absent content item {bar.ActionName}.");
               return View("Log", new LogViewModel(_logger.Log, null, null));
            }
            #endregion

            #region batch creation
            var createAlias = "batch-create";
            var createParameters = new Dictionary<string, string> {
               { "ReportId", report.ContentItem.Id.ToString() },
               { "TaskId", bulkAction.Id.ToString() },
               { "Description", report.Process.Actions.First(a=>a.Name == bar.ActionName).Description }
            };

            var create = await _taskService.Validate(createAlias, false, createParameters);

            if (create.Fails()) {
               return create.ActionResult;
            }

            await _taskService.RunAsync(create.Process, _logger);
            if (create.Process.Status != 200) {
               _logger.Warn(() => $"User {user} received error running action {createAlias}.");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }

            var entity = create.Process.Entities.FirstOrDefault();

            if (entity == null) {
               _logger.Error(() => $"The {createAlias} bulk action is missing an entity.");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }

            if (!entity.Rows.Any()) {
               _logger.Error(() => $"The {createAlias} bulk action didn't produce a batch record.");
               return View("Log", new LogViewModel(_logger.Log, create.Process, create.ContentItem));
            }
            #endregion

            #region batch writing
            var writeAlias = "batch-write";
            var writeParameters = new Dictionary<string, string>();
            foreach (var field in entity.GetAllOutputFields()) {
               writeParameters[field.Alias] = entity.Rows[0][field.Alias].ToString();
            }

            var write = await _taskService.Validate(writeAlias, false, writeParameters);

            if (write.Fails()) {
               return write.ActionResult;
            }

            // potential memory problem (could be solved by merging report and batch write into one process)
            var writeEntity = write.Process.Entities.First();
            var batchValueField = writeEntity.Fields.LastOrDefault(f => f.Input && f.Output);

            if (batchValueField == null) {
               _logger.Error(() => $"Could not identify batch value field in {writeAlias}.");
               return View("Log", new LogViewModel(_logger.Log, write.Process, write.ContentItem));
            }

            if (bar.ActionCount == 0) {
               var batchProcess = _reportService.LoadForBatch(report.ContentItem, _logger);

               await _taskService.RunAsync(batchProcess, _logger);
               foreach (var batchRow in batchProcess.Entities.First().Rows) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchRow[report.Part.BulkActionValueField.Text];
                  writeEntity.Rows.Add(row);
               }
            } else {
               foreach (var batchValue in bar.Records) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchValue;
                  writeEntity.Rows.Add(row);
               }
            }

            await _taskService.RunAsync(write.Process, _logger);
            #endregion

            writeParameters.Add("ContentItemId", bar.ContentItemId);
            writeParameters.Add("ActionName", bar.ActionName);
            return RedirectToAction("Review", ParametersToRouteValues(writeParameters));

         } else {
            _logger.Warn(() => $"User {user} called missing action {bar.ActionName} in {report.ContentItem.DisplayText}.");
         }

         return View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem));

      }

      [HttpGet]
      public async Task<ActionResult> Review(string contentItemId) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User.Identity.Name ?? "Anonymous";

         var report = await _reportService.Validate(contentItemId);

         if (report.Fails()) {
            return report.ActionResult;
         }

         var batchSummaryAlias = "batch-summary";
         var batchSummary = await _taskService.GetByIdOrAliasAsync(batchSummaryAlias);

         Process batchSummaryProcess = null;
         if (batchSummary != null) {
            batchSummaryProcess = _taskService.LoadForTask(batchSummary, _logger);
            if (batchSummaryProcess.Status == 200) {
               await _taskService.RunAsync(batchSummaryProcess, _logger);
            }
         }

         var actionName = HttpContext.Request.Query["ActionName"].ToString();
         Process bulkActionProcess = null;
         if (report.Process.Actions.Any(a => a.Name == actionName)) {

            #region BulkAction
            var bulkAction = await _taskService.GetByIdOrAliasAsync(actionName);

            if (bulkAction == null) {
               _logger.Warn(() => $"User {user} requested absent content item {actionName}.");
               return View("Log", new LogViewModel(_logger.Log, null, null));
            }

            bulkActionProcess = _taskService.LoadForTask(bulkAction, _logger);
            if (bulkActionProcess.Status != 200) {
               _logger.Warn(() => $"User {user} received error trying to load bulk action {bulkAction.DisplayText}.");
               return View("Log", new LogViewModel(_logger.Log, bulkActionProcess, bulkAction));
            }
            #endregion
         } else {
            _logger.Warn(() => $"User {user} called missing action {actionName} in {report.ContentItem.DisplayText}.");
         }

         return View(new BulkActionViewModel(batchSummaryProcess, bulkActionProcess));
      }

      private dynamic ParametersToRouteValues(IDictionary<string, string> parameters) {
         var routeValues = new ExpandoObject();
         var editable = (ICollection<KeyValuePair<string, object>>)routeValues;
         foreach (var kvp in parameters) {
            editable.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
         }
         dynamic d = routeValues;
         return d;
      }

   }
}
