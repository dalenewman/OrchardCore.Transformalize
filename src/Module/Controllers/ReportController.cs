using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Services.Contracts;
using Module.ViewModels;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;
using Module.Services;
using Module.Models;
using System.Collections.Generic;

namespace Module.Controllers {
   public class ReportController : Controller {

      private readonly IReportService _reportService;
      private readonly ISlugService _slugService;
      private readonly CombinedLogger<ReportController> _logger;

      public ReportController(
         IReportService reportService,
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

         await _reportService.RunAsync(report.Process);
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

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var request = new TransformalizeRequest(contentItemId, user) { Format = format };
         var report = await _reportService.Validate(request);

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _reportService.RunAsync(report.Process);

         report.Process.Connections.Clear();

         return new ContentResult() { Content = report.Process.Serialize(), ContentType = request.ContentType };
      }

      [HttpGet]
      public async Task<ActionResult> StreamJson(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForExport(contentItem);

         if (process.Status != 200) {
            return Problem();
         }

         var o = process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "json";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".json";

         Response.ContentType = "application/json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamGeoJson(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForExport(contentItem);

         if (process.Status != 200) {
            return Problem();
         }

         var part = contentItem.As<TransformalizeReportPart>();

         var o = process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "geojson";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".geo.json";

         // todo: these will have to be put in report part
         var suppress = new HashSet<string>() { part.BulkActionValueField.Text, "geojson-color", "geojson-description" };
         var coordinates = new HashSet<string>() { "latitude", "longitude" };

         foreach (var entity in process.Entities) {
            foreach (var field in entity.GetAllFields()) {
               if (suppress.Contains(field.Alias)) {
                  field.Output = false;
                  field.Property = false;
                  field.Alias += "Suppressed";
               } else if (coordinates.Contains(field.Alias)) {
                  field.Property = field.Export == "true";
               } else {
                  field.Property = field.Property || field.Output && field.Export == "defer" || field.Export == "true";
               }
            }
         }

         Response.ContentType = "application/vnd.geo+json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamCsv(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         if (!_reportService.CanAccess(contentItem)) {
            return Unauthorized();
         }

         var process = _reportService.LoadForExport(contentItem);

         if (process.Status != 200) {
            return Problem();
         }

         var o = process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "file";
         o.Delimiter = ",";
         o.TextQualifier = "\"";
         o.File = _slugService.Slugify(contentItem.As<TitlePart>().Title) + ".csv";

         Response.ContentType = "application/csv";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);


         await _reportService.RunAsync(process);

         return new EmptyResult();

      }
   }
}
