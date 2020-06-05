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
using System.Linq;
using Module.Models;

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

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var report = await _reportService.Validate(new TransformalizeRequest(contentItemId, user));

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _reportService.RunAsync(report.Process, _logger);
         if (report.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem));
         }
         
         return log ? View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem)) : View(new ReportViewModel(report.Process, report.ContentItem));

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

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForReport(contentItem, _logger, format);
         var contentType = format == "json" ? "application/json" : "application/xml";

         if (process.Status != 200) {
            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         if (!process.Parameters.All(p=>p.Valid)) {
            foreach(var parameter in process.Parameters.Where(p => !p.Valid)) {
               _logger.Warn(() => parameter.Message);
            }
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
