using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Logging;
using Transformalize.Context;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using System.Collections.Generic;
using Module.Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Module.Controllers {

   public class BulkActionController : Controller {

      private readonly ITaskService _taskService;
      private readonly IReportService _reportService;
      private readonly ILogger<BulkActionController> _logger;

      public BulkActionController(
         ITaskService taskService,
         IReportService reportService,
         ILogger<BulkActionController> logger
      ) {
         _taskService = taskService;
         _reportService = reportService;
         _logger = logger;
      }

      [HttpPost]
      public async Task<ActionResult> Index(BulkActionRequest bar) {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var report = await _reportService.GetByIdOrAliasAsync(bar.ContentItemId);

         if (report == null) {
            _logger.LogWarning($"User {user} requested absent content item {bar.ContentItemId}.");
            return NotFound();
         }
         var part = report.ContentItem.As<TransformalizeReportPart>();

         if (part == null) {
            _logger.LogWarning($"User {user} requested absent non-report content type {report.DisplayText}.");
            return BadRequest();
         }

         if (!_reportService.CanAccess(report)) {
            _logger.LogWarning($"User {user} is unauthorized to access {report.DisplayText}.");
            return Unauthorized();
         }

         var logger = new MemoryLogger(Transformalize.Contracts.LogLevel.Info);
         var reportProcess = _reportService.LoadForReport(report, logger);

         if (reportProcess.Status != 200) {
            _logger.LogWarning($"User {user} received error trying to load report {report.DisplayText}.");
            return View(new TaskViewModel(reportProcess, report));
         }

         var context = new PipelineContext(logger, reportProcess);
         var referrer = Request.Headers.ContainsKey("Referer") ? Request.Headers["Referer"].ToString() : Url.Action("Index", "Report", new { bar.ContentItemId });

         // confirm we have an action registered in the report
         if (reportProcess.Actions.Any(a => a.Name == bar.ActionName)) {

            #region the bulk action in question (not doing anything yet)
            var bulkAction = await _taskService.GetByIdOrAliasAsync(bar.ActionName);

            if (bulkAction == null) {
               _logger.LogWarning($"User {user} requested absent content item {bar.ActionName}.");
               return NotFound();
            }

            var bulkActionProcess = _taskService.LoadForTask(bulkAction, logger);
            if (bulkActionProcess.Status != 200) {
               _logger.LogWarning($"User {user} received error trying to load bulk action {bulkAction.DisplayText}.");
               return View(new TaskViewModel(bulkActionProcess, bulkAction));
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
               _logger.LogWarning($"User {user} requested absent content item {batchCreateAlias}.");
               return NotFound();
            }
            var batchCreateProcess = _taskService.LoadForTask(batchCreate, logger, batchCreateParameters);
            if (batchCreateProcess.Status != 200) {
               _logger.LogWarning($"User {user} received error trying to load bulk action {batchCreateAlias}.");
               return View(new TaskViewModel(batchCreateProcess, batchCreate));
            }
            await _taskService.RunAsync(batchCreateProcess, logger);
            if (batchCreateProcess.Status != 200) {
               _logger.LogWarning($"User {user} received error running action {batchCreateAlias}.");
               return View(new TaskViewModel(batchCreateProcess, batchCreate));
            }

            var entity = batchCreateProcess.Entities.FirstOrDefault();

            if (entity == null) {
               _logger.LogError($"The {batchCreateAlias} bulk action is missing an entity.");
               return View(new TaskViewModel(batchCreateProcess, batchCreate));
            }

            if (!entity.Rows.Any()) {
               _logger.LogError($"The {batchCreateAlias} bulk action didn't produce a batch record.");
               return View(new TaskViewModel(batchCreateProcess, batchCreate));
            }
            #endregion

            #region batch writing
            var batchWriteAlias = "batch-write";
            var batchWriteParameters = new Dictionary<string, string>();
            foreach (var field in entity.GetAllOutputFields()) {
               batchWriteParameters[field.Alias] = entity.Rows[0][field.Alias].ToString();
            }
            var batchWrite = await _taskService.GetByIdOrAliasAsync(batchWriteAlias);
            var batchWriteProcess = _taskService.LoadForTask(batchWrite, logger, batchWriteParameters);
            if (batchWriteProcess.Status != 200) {
               _logger.LogWarning($"User {user} received error trying to load bulk action {batchWriteAlias}.");
               return View(new TaskViewModel(batchWriteProcess, batchWrite));
            }

            // potential memory problem (could be solved by merging report and batch write into one process)
            var writeEntity = batchWriteProcess.Entities.First();
            var batchValueField = writeEntity.Fields.LastOrDefault(f => f.Input && f.Output);

            if(batchValueField == null) {
               _logger.LogError($"Could not identify batch value field in {batchWriteAlias}.");
               return View(new TaskViewModel(batchWriteProcess, batchWrite));
            }

            if (bar.ActionCount == 0) {
               var batchProcess = _reportService.LoadForBatch(report, logger);

               await _taskService.RunAsync(batchProcess, logger);
               foreach(var batchRow in batchProcess.Entities.First().Rows) {
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

            await _taskService.RunAsync(batchWriteProcess, logger);
            #endregion

         } else {
            _logger.LogWarning($"User {user} called missing action {bar.ActionName} in {report.DisplayText}.");
         }

         reportProcess.Log = logger.Log;
         return View(new TaskViewModel(reportProcess, report));

      }

      public TaskViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new TaskViewModel(new Process { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(Transformalize.Contracts.LogLevel.Error, null, message) } }, contentItem);
      }

   }
}
