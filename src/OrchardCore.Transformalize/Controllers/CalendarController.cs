using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class CalendarController : Controller {

      private readonly IReportService _reportService;
      private readonly CombinedLogger<ReportController> _logger;
      private readonly ISettingsService _settings;

      public CalendarController(
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

         var request = new TransformalizeRequest(contentItemId) { Mode = "calendar" };
         var calendar = await _reportService.Validate(request);

         if (calendar.Fails()) {
            return calendar.ActionResult;
         }

         await _reportService.RunAsync(calendar.Process, null);

         if (calendar.Process.Status != 200) {
            return View("Log", new LogViewModel(_logger.Log, calendar.Process, calendar.ContentItem));
         }

         return View(new ReportViewModel(calendar.Process, calendar.ContentItem, contentItemId) { Settings = _settings.Settings });

      }

      [HttpGet]
      public async Task<ActionResult> Stream(string contentItemId) {

         var request = new TransformalizeRequest(contentItemId) { Mode = "stream-calendar" };
         var map = await _reportService.Validate(request);

         if (map.Fails()) {
            return map.ActionResult;
         }

         var syncIOFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
         if (syncIOFeature != null) {
            syncIOFeature.AllowSynchronousIO = true;
         }

         Response.ContentType = "application/json";

         StreamWriter sw;

         //await using ((sw = new StreamWriter(Response.Body)).ConfigureAwait(false)) {
         //   await _reportService.RunAsync(stream.Process, sw).ConfigureAwait(false);
         //}

         using (sw = new StreamWriter(Response.Body)) {
            _reportService.Run(map.Process, sw);
         }

         return new EmptyResult();

      }

   }
}
