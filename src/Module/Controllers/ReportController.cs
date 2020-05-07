using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;
using Transformalize.Context;
using Cfg.Net.Serializers;

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
            return View("Log", _reportService.GetErrorModel(contentItem, "Access Denied."));
         }

         var part = contentItem.As<TransformalizeReportPart>();

         if (part != null) {

            var process = _reportService.Load(contentItem, part.Arrangement.Arrangement, logger);

            if (process.Errors().Any()) {
               process.Log = logger.Log;
               return View("Log", new ReportViewModel(process, contentItem));
            }

            if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
               return View(new ReportViewModel(process, contentItem));
            }

            try {
               await _reportService.RunAsync(process, logger);
               if (logger.Log.Any(l => l.LogLevel == LogLevel.Error)) {
                  process.Status = 500;
                  process.Message = "Error";
                  process.Log = logger.Log;
                  return View("Log", new ReportViewModel(process, contentItem));
               }
               process.Status = 200;
               process.Message = "Ok";
            } catch (System.Exception ex) {
               process.Status = 500;
               process.Message = ex.Message;
               process.Log.Add(new LogEntry(LogLevel.Error, null, ex.Message));
               process.Log.AddRange(logger.Log);
               return View("Log", new ReportViewModel(process, contentItem));
            }

            if (Request.Query["log"].ToString() == "1") {
               process.Log = logger.Log;
               return View("Log", new ReportViewModel(process, contentItem));
            } else {
               return View(new ReportViewModel(process, contentItem));
            }

         }

         return View("Log", _reportService.GetErrorModel(contentItem, $"Unable to handle content type: {contentItem.ContentType}."));
      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string type = "json") {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var contentType = type == "json" ? "application/json" : "text/xml";
         var serializer = type == "json" ? new JsonSerializer() : null;

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var part = contentItem.As<TransformalizeReportPart>();

         if (part != null) {

            var process = _reportService.Load(contentItem, part.Arrangement.Arrangement, logger, serializer);
            var context = new PipelineContext(logger, process);

            if (process.Errors().Any()) {
               process.Log = logger.Log;
               process.Connections.Clear();
               return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
            }

            if (_reportService.IsMissingRequiredParameters(process.Parameters)) {
               context.Error("Missing required parameter(s)");
               process.Log = logger.Log;
               process.Message = "Error";
               process.Status = 500;
               process.Connections.Clear();
               return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
            }

            try {
               await _reportService.RunAsync(process, logger);
               if (logger.Log.Any(l => l.LogLevel == LogLevel.Error)) {
                  process.Status = 500;
                  process.Message = "Error";
                  process.Log = logger.Log;
                  process.Connections.Clear();
                  return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
               }
               process.Status = 200;
               process.Message = "Ok";
            } catch (System.Exception ex) {
               process.Status = 500;
               process.Message = "Error";
               context.Error(ex.Message);
               process.Log.AddRange(logger.Log);
               process.Connections.Clear();
               return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
            }

            process.Connections.Clear();
            return new ContentResult() { Content = process.Serialize(), ContentType = contentType };
         }

         return BadRequest();
      }

      [HttpGet]
      public async Task<ActionResult> Json(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var part = contentItem.As<TransformalizeReportPart>();

         var logger = new MemoryLogger(LogLevel.Error);

         if (part != null) {

            var process = _reportService.LoadForExport(part.Arrangement.Arrangement, logger);

            if (process.Errors().Any()) {
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

         return Problem();
      }

      [HttpGet]
      public async Task<ActionResult> Csv(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var part = contentItem.As<TransformalizeReportPart>();

         var logger = new MemoryLogger(LogLevel.Error);

         if (part != null) {

            var process = _reportService.LoadForExport(part.Arrangement.Arrangement, logger);

            if (process.Errors().Any()) {
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

         return Problem();
      }


   }
}
