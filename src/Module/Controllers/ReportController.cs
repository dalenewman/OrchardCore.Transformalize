using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;
using Transformalize.Context;
using Transformalize.Configuration;
using System.Collections.Generic;

namespace Module.Controllers {
   public class ReportController : Controller {

      private readonly IReportService _reportService;
      private readonly ISlugService _slugService;

      public ReportController(
         IReportService reportService,
         ISlugService slugService
      ) {
         _reportService = reportService;
         _slugService = slugService;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_reportService.CanAccess(contentItem)) {
            return View("Log", GetErrorModel(contentItem, "Access Denied."));
         }

         var process = _reportService.LoadForReport(contentItem, logger);

         if (process.Status != 200) {
            return View("Log", new ReportViewModel(process, contentItem));
         }

         if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
            return View(new ReportViewModel(process, contentItem));
         }

         await _reportService.RunAsync(process, logger);
         if (process.Status != 200) {
            return View("Log", new ReportViewModel(process, contentItem));
         }

         if (Request.Query["log"].ToString() == "1") {
            process.Log = logger.Log;
            return View("Log", new ReportViewModel(process, contentItem));
         } else {
            return View(new ReportViewModel(process, contentItem));
         }

      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         // important: since you're exposing the configuration, 
         // always clear out the connections before sending to the client

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var contentType = format == "json" ? "application/json" : "application/xml";

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForReport(contentItem, logger, format);

         if (process.Status != 200) {
            process.Log = logger.Log;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         var context = new PipelineContext(logger, process);

         if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
            context.Error("Missing required parameter(s)");
            process.Log = logger.Log;
            process.Message = "Error";
            process.Status = 500;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         await _reportService.RunAsync(process, logger);
         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         process.Connections.Clear();
         return new ContentResult() { Content = process.Serialize(), ContentType = contentType };

      }

      [HttpGet]
      public async Task<ActionResult> SaveAsJson(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var logger = new MemoryLogger(LogLevel.Error);

         var process = _reportService.LoadForExport(contentItem, logger);

         if (process.Status != 200) {
            return Problem();
         }

         var o = process.Output();
         o.Stream = true;
         o.Provider = "json";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".json";

         Response.ContentType = "application/json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(process, logger);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> SaveAsCsv(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var logger = new MemoryLogger(LogLevel.Error);


         var process = _reportService.LoadForExport(contentItem, logger);

         if (process.Status != 200) {
            return Problem();
         }

         var o = process.Output();
         o.Stream = true;
         o.Provider = "file";
         o.Delimiter = ",";
         o.TextQualifier = "\"";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".csv";

         Response.ContentType = "application/csv";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);


         await _reportService.RunAsync(process, logger);

         return new EmptyResult();

      }

      public ReportViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new ReportViewModel(new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(LogLevel.Error, null, message) } }, contentItem);
      }


   }
}
