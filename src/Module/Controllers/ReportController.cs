using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Logging;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;
using Transformalize.Configuration;
using System.Collections.Generic;
using Module.Services;

namespace Module.Controllers {
   public class ReportController : Controller {

      private readonly IReportService<ReportController> _reportService;
      private readonly ISlugService _slugService;
      private readonly CombinedLogger<ReportController> _logger;

      public ReportController(
         IReportService<ReportController> reportService,
         ISlugService slugService,
         CombinedLogger<ReportController> logger
      ) {
         _reportService = reportService;
         _slugService = slugService;
         _logger = logger;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId, bool log = false) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return View("Log", GetErrorModel(contentItem, "Access Denied."));
         }

         var process = _reportService.LoadForReport(contentItem, _logger);

         if (process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, process, contentItem));
         }

         if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
            return View(new LogViewModel(_logger.Log, process, contentItem));
         }

         await _reportService.RunAsync(process, _logger);
         if (process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, process, contentItem));
         }
         
         return log ? View("Log", new LogViewModel(_logger.Log, process, contentItem)) : View(new ReportViewModel(process, contentItem));

      }

      [HttpGet]
      public async Task<ActionResult> Log(string contentItemId) {
         return await Index(contentItemId, log: true);
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

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForReport(contentItem, _logger, format);

         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
            _logger.Error(() => "Missing required parameter(s)");
            process.Message = "Error";
            process.Status = 500;
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         await _reportService.RunAsync(process, _logger);
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

         var process = _reportService.LoadForExport(contentItem, _logger);

         if (process.Status != 200) {
            return Problem();
         }

         var o = process.Output();
         o.Stream = true;
         o.Provider = "json";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".json";

         Response.ContentType = "application/json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(process, _logger);

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

         var process = _reportService.LoadForExport(contentItem, _logger);

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


         await _reportService.RunAsync(process, _logger);

         return new EmptyResult();

      }

      public LogViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new LogViewModel(_logger.Log, new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(Transformalize.Contracts.LogLevel.Error, null, message) } }, contentItem);
      }


   }
}
