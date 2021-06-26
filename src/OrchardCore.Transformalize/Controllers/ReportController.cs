using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace TransformalizeModule.Controllers {

   [Authorize]
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

         var report = await _reportService.Validate(new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name));

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _reportService.RunAsync(report.Process);
         if (report.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem));
         }

         return log ? View("Log", new LogViewModel(_logger.Log, report.Process, report.ContentItem)) : View(new ReportViewModel(report.Process, report.ContentItem, contentItemId));

      }

      [HttpGet]
      public async Task<ActionResult> Log(string contentItemId) {
         return await Index(contentItemId, log: true);
      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { Format = format };
         var report = await _reportService.Validate(request);

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _reportService.RunAsync(report.Process);

         report.Process.Connections.Clear();

         return new ContentResult { 
            Content = report.Process.Serialize(), 
            ContentType = request.ContentType 
         };
      }

      [HttpGet]
      public async Task<ActionResult> StreamJson(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { Mode = "stream" };
         var stream = await _reportService.Validate(request).ConfigureAwait(false);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "json";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".json";

         PrepareResponse(Response, "application/csv", o.File);

         _reportService.Run(stream.Process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamGeoJson(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { Mode = "stream" };
         var stream = await _reportService.Validate(request).ConfigureAwait(false);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "geojson";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".geo.json";

         // todo: these will have to be put in report part
         var suppress = new HashSet<string>() { stream.Part.BulkActionValueField.Text, stream.Part.MapColorField.Text, stream.Part.MapDescriptionField.Text };
         var coordinates = new HashSet<string>() { stream.Part.MapLatitudeField.Text, stream.Part.MapLongitudeField.Text };

         foreach (var entity in stream.Process.Entities) {
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

         PrepareResponse(Response, "application/csv", o.File);

         _reportService.Run(stream.Process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamCsv(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { Mode = "stream" };
         var stream = await _reportService.Validate(request).ConfigureAwait(false);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "file";
         o.Delimiter = ",";
         o.TextQualifier = "\"";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".csv";

         PrepareResponse(Response, "application/csv", o.File);

         _reportService.Run(stream.Process);

         return new EmptyResult();

      }

      private static void PrepareResponse(HttpResponse response, string contentType, string fileName) {
         response.Clear();
         response.ContentType = contentType;
         response.Headers.Add("content-disposition", "attachment; filename=" + fileName);
         response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
         response.Headers.Add("Pragma", "no-cache");
         response.Headers.Add("Expires", "0");
      }
   }
}
