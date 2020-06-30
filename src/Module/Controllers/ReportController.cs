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
using Microsoft.AspNetCore.Authorization;

namespace Module.Controllers {

   [Authorize]
   public class ReportController : Controller {

      private readonly IReportService _reportService;
      private readonly ISlugService _slugService;
      private readonly CombinedLogger<ReportController> _logger;
      private readonly ISettingsService _settings;

      public ReportController(
         IReportService reportService,
         ISlugService slugService,
         ISettingsService settings,
         CombinedLogger<ReportController> logger
      ) {
         _reportService = reportService;
         _slugService = slugService;
         _settings = settings;
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
      public async Task<ActionResult> Map(string contentItemId) {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var request = new TransformalizeRequest(contentItemId, user) { Mode = "map" };
         var map = await _reportService.Validate(request);

         if (map.Fails()) {
            return map.ActionResult;
         }

         if (string.IsNullOrEmpty(_settings.Settings.MapBoxToken)) {
            _logger.Warn(() => $"User {request.User} requested map without mapbox token {request.ContentItemId}.");

            map.Process.Status = 404;
            map.Process.Message = "MapBox Token Not Found";

            return View("Log", new LogViewModel(_logger.Log, map.Process, map.ContentItem));
         }

         await _reportService.RunAsync(map.Process);

         if (map.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, map.Process, map.ContentItem));
         }

         return View(new ReportViewModel(map.Process, map.ContentItem) { Settings = _settings.Settings });

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

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var request = new TransformalizeRequest(contentItemId, user) { Mode = "stream" };
         var stream = await _reportService.Validate(request);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "json";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".json";

         Response.ContentType = "application/json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(stream.Process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamGeoJson(string contentItemId) {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var request = new TransformalizeRequest(contentItemId, user) { Mode = "stream" };
         var stream = await _reportService.Validate(request);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "geojson";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".geo.json";

         // todo: these will have to be put in report part
         var suppress = new HashSet<string>() { stream.Part.BulkActionValueField.Text, "geojson-color", "geojson-description" };
         var coordinates = new HashSet<string>() { "latitude", "longitude" };

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

         Response.ContentType = "application/vnd.geo+json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         await _reportService.RunAsync(stream.Process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamMap(string contentItemId) {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var request = new TransformalizeRequest(contentItemId, user) { Mode = "stream-map" };
         var map = await _reportService.Validate(request);

         if (map.Fails()) {
            return map.ActionResult;
         }

         Response.ContentType = "application/vnd.geo+json";

         await _reportService.RunAsync(map.Process);

         return new EmptyResult();

      }

      [HttpGet]
      public async Task<ActionResult> StreamCsv(string contentItemId) {

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
         var request = new TransformalizeRequest(contentItemId, user) { Mode = "stream" };
         var stream = await _reportService.Validate(request);

         if (stream.Fails()) {
            return stream.ActionResult;
         }

         var o = stream.Process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "file";
         o.Delimiter = ",";
         o.TextQualifier = "\"";
         o.File = _slugService.Slugify(stream.ContentItem.As<TitlePart>().Title) + ".csv";

         Response.ContentType = "application/csv";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);


         await _reportService.RunAsync(stream.Process);

         return new EmptyResult();

      }
   }
}
