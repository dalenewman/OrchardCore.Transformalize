using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class MapController : Controller {

      private readonly IReportService _reportService;
      private readonly CombinedLogger<ReportController> _logger;
      private readonly ISettingsService _settings;

      public MapController(
         IReportService reportService,
         ISettingsService settings,
         CombinedLogger<ReportController> logger
      ) {
         _reportService = reportService;
         _settings = settings;
         _logger = logger;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId) { Mode = "map" };
         var map = await _reportService.Validate(request);

         if (map.Fails()) {
            return map.ActionResult;
         }

         if (string.IsNullOrEmpty(_settings.Settings.MapBoxToken)) {
            _logger.Warn(() => $"User {HttpContext.User.Identity.Name} requested map without mapbox token {request.ContentItemId}.");

            map.Process.Status = 404;
            map.Process.Message = "MapBox Token Not Found";

            return View("Log", new LogViewModel(_logger.Log, map.Process, map.ContentItem));
         }

         await _reportService.RunAsync(map.Process, null);

         if (map.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, map.Process, map.ContentItem));
         }

         return View(new ReportViewModel(map.Process, map.ContentItem, HttpContext.Request.Query, contentItemId) { Settings = _settings.Settings, EnableInlineParameters = false });

      }

      [HttpGet]
      public async Task<ActionResult> Stream(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId) { Mode = "stream-map" };
         var map = await _reportService.Validate(request);

         if (map.Fails()) {
            return map.ActionResult;
         }

         Response.ContentType = "application/vnd.geo+json";

         StreamWriter sw;
         await using (sw = new StreamWriter(Response.Body)) {
            await _reportService.RunAsync(map.Process, sw);
         }

         return new EmptyResult();

      }

   }
}
