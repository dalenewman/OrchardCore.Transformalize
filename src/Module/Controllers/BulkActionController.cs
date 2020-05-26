using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Context;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using System.Collections.Generic;
using Module.Models;

namespace Module.Controllers {

   public class BulkActionController : Controller {

      private readonly ITaskService _taskService;
      private readonly IReportService _reportService;
      private readonly IParameterService _parameterService;

      public BulkActionController(
         ITaskService taskService,
         IReportService reportService,
         IParameterService parameterService
      ) {
         _taskService = taskService;
         _reportService = reportService;
         _parameterService = parameterService;
      }

      [HttpPost]
      public async Task<ActionResult> Index(BulkActionRequest bar) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(bar.ContentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return View(GetErrorModel(contentItem, "Access Denied."));
         }

         var logger = new MemoryLogger(LogLevel.Info);
         var process = _reportService.LoadForReport(contentItem, logger);

         if (process.Status != 200) {
            return View(new TaskViewModel(process, contentItem));
         }

         var context = new PipelineContext(logger, process);
         var referrer = Request.Headers.ContainsKey("Referer") ? Request.Headers["Referer"].ToString() : Url.Action("Index", "Report", new { ContentItemId = bar.ContentItemId });

         context.Info("Parameters");
         var parameters = _parameterService.GetParameters();
         foreach(var kv in parameters) {
            context.Info($"{kv.Key} = {kv.Value}");
         }

         // confirm we have action, then loadForTask
         // currently if you access report you inherit access to action
         // create batch using batch task, getting batch id, storing user, report, action, date, count or all, writes to internal output when can be read here
         // write batch using batch write service 
         //   (has to either write values submitted in request Records, or run the report's query for batch with parameters submitted here)

         process.Log = logger.Log;
         return View(new TaskViewModel(process, contentItem));

      }

      public TaskViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new TaskViewModel(new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(LogLevel.Error, null, message) } }, contentItem);
      }

   }
}
