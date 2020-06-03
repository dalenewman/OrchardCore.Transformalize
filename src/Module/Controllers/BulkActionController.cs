using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using OrchardCore.ContentManagement;
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
         var report = await _reportService.GetByIdOrAliasAsync(bar.ContentItemId);

         if (report == null) {
            _logger.Warn(() => $"User {user} requested absent content item {bar.ContentItemId}.");
            return View("Log", new LogViewModel(_logger.Log, null, null));
         }

         if (!_reportService.CanAccess(report)) {
            _logger.Warn(() => $"User {user} is unauthorized to access {report.DisplayText}.");
            return View("Log", new LogViewModel(_logger.Log, null, null));
         }

         var part = report.ContentItem.As<TransformalizeReportPart>();
         if (part == null) {
            _logger.Warn(() => $"User {user} requested incompatible content type {report.DisplayText}.");
            return BadRequest();
         }

         var reportProcess = _reportService.LoadForReport(report, _logger);
         if (reportProcess.Status != 200) {
            _logger.Warn(() => $"User {user} received error trying to load report {report.DisplayText}.");
            return View("Log", new LogViewModel(_logger.Log, reportProcess, report));
         }

         var referrer = Request.Headers.ContainsKey("Referer") ? Request.Headers["Referer"].ToString() : Url.Action("Index", "Report", new { bar.ContentItemId });

         // confirm we have an action registered in the report
         if (reportProcess.Actions.Any(a => a.Name == bar.ActionName)) {

            #region the bulk action in question (not doing anything yet)
            var bulkAction = await _taskService.GetByIdOrAliasAsync(bar.ActionName);

            if (bulkAction == null) {
               _logger.Warn(() => $"User {user} requested absent content item {bar.ActionName}.");
               return View("Log", new LogViewModel(_logger.Log, null, null));
            }
            #endregion

            #region batch creation
            var batchCreateAlias = "batch-create";
            var batchCreateParameters = new Dictionary<string, string> {
               { "ReportId", report.Id.ToString() },
               { "TaskId", bulkAction.Id.ToString() },
               { "Description", reportProcess.Actions.First(a=>a.Name == bar.ActionName).Description }
            };
            var batchCreate = await _taskService.GetByIdOrAliasAsync(batchCreateAlias);
            if (batchCreate == null) {
               _logger.Warn(() => $"User {user} requested absent content item {batchCreateAlias}.");
               return NotFound();
            }
            var batchCreateProcess = _taskService.LoadForTask(batchCreate, _logger, batchCreateParameters);
            if (batchCreateProcess.Status != 200) {
               _logger.Warn(() => $"User {user} received error trying to load bulk action {batchCreateAlias}.");
               return View("Log", new LogViewModel(_logger.Log, batchCreateProcess, batchCreate));
            }
            await _taskService.RunAsync(batchCreateProcess, _logger);
            if (batchCreateProcess.Status != 200) {
               _logger.Warn(() => $"User {user} received error running action {batchCreateAlias}.");
               return View("Log", new LogViewModel(_logger.Log, batchCreateProcess, batchCreate));
            }

            var entity = batchCreateProcess.Entities.FirstOrDefault();

            if (entity == null) {
               _logger.Error(() => $"The {batchCreateAlias} bulk action is missing an entity.");
               return View("Log", new LogViewModel(_logger.Log, batchCreateProcess, batchCreate));
            }

            if (!entity.Rows.Any()) {
               _logger.Error(() => $"The {batchCreateAlias} bulk action didn't produce a batch record.");
               return View("Log", new LogViewModel(_logger.Log, batchCreateProcess, batchCreate));
            }
            #endregion

            #region batch writing
            var batchWriteAlias = "batch-write";
            var batchWriteParameters = new Dictionary<string, string>();
            foreach (var field in entity.GetAllOutputFields()) {
               batchWriteParameters[field.Alias] = entity.Rows[0][field.Alias].ToString();
            }
            var batchWrite = await _taskService.GetByIdOrAliasAsync(batchWriteAlias);
            var batchWriteProcess = _taskService.LoadForTask(batchWrite, _logger, batchWriteParameters);
            if (batchWriteProcess.Status != 200) {
               _logger.Warn(() => $"User {user} received error trying to load bulk action {batchWriteAlias}.");
               return View("Log", new LogViewModel(_logger.Log, batchWriteProcess, batchWrite));
            }

            // potential memory problem (could be solved by merging report and batch write into one process)
            var writeEntity = batchWriteProcess.Entities.First();
            var batchValueField = writeEntity.Fields.LastOrDefault(f => f.Input && f.Output);

            if (batchValueField == null) {
               _logger.Error(() => $"Could not identify batch value field in {batchWriteAlias}.");
               return View("Log", new LogViewModel(_logger.Log, batchWriteProcess, batchWrite));
            }

            if (bar.ActionCount == 0) {
               var batchProcess = _reportService.LoadForBatch(report, _logger);

               await _taskService.RunAsync(batchProcess, _logger);
               foreach (var batchRow in batchProcess.Entities.First().Rows) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchRow[part.BulkActionValueField.Text];
                  writeEntity.Rows.Add(row);
               }
            } else {
               foreach (var batchValue in bar.Records) {
                  var row = new Transformalize.Impl.CfgRow(new[] { batchValueField.Alias });
                  row[batchValueField.Alias] = batchValue;
                  writeEntity.Rows.Add(row);
               }
            }

            await _taskService.RunAsync(batchWriteProcess, _logger);
            #endregion

            batchWriteParameters.Add("ContentItemId", bar.ContentItemId);
            batchWriteParameters.Add("ActionName", bar.ActionName);
            return RedirectToAction("Review", ParametersToRouteValues(batchWriteParameters));

         } else {
            _logger.Warn(() => $"User {user} called missing action {bar.ActionName} in {report.DisplayText}.");
         }

         return View("Log", new LogViewModel(_logger.Log, reportProcess, report));

      }

      [HttpGet]
      public async Task<ActionResult> Review(string contentItemId) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User.Identity.Name ?? "Anonymous";
         var report = await _reportService.GetByIdOrAliasAsync(contentItemId);

         if (report == null) {
            _logger.Warn(() => $"User {user} requested absent content item {contentItemId}.");
            return View("Log", new LogViewModel(_logger.Log, null, null));
         }

         if (!_reportService.CanAccess(report)) {
            _logger.Warn(() => $"User {user} is unauthorized to access {report.DisplayText}.");
            return View("Log", new LogViewModel(_logger.Log, null, null));
         }

         var part = report.ContentItem.As<TransformalizeReportPart>();
         if (part == null) {
            _logger.Warn(() => $"User {user} requested incompatible content type {report.DisplayText}.");
            return BadRequest();
         }

         var reportProcess = _reportService.LoadForReport(report, _logger);
         if (reportProcess.Status != 200) {
            _logger.Warn(() => $"User {user} received error trying to load report {report.DisplayText}.");
            return View("Log", new LogViewModel(_logger.Log, reportProcess, report));
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
         if (reportProcess.Actions.Any(a => a.Name == actionName)) {

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
            _logger.Warn(() => $"User {user} called missing action {actionName} in {report.DisplayText}.");
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
