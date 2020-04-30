using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;

namespace Module.Controllers {
   public class ReportController : Controller {

      private readonly IReportService _reportService;

      public ReportController(
         IReportService reportService         
      ) {
         _reportService = reportService;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await _reportService.GetByIdOrAliasAsync(contentItemId);
         if(contentItem == null) {
            return NotFound();
         }

         var logger = new MemoryLogger(LogLevel.Info);

         if (!_reportService.CanAccess(contentItem)) {
            return View("Log", _reportService.GetErrorModel(contentItem,"Access Denied."));
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
               if (logger.Log.Any(l=>l.LogLevel == LogLevel.Error)) {
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

            if(Request.Query["log"].ToString() == "1") {
               process.Log = logger.Log;
               return View("Log", new ReportViewModel(process, contentItem));
            } else {
               return View(new ReportViewModel(process, contentItem));
            }            

         }

         return View("Log", _reportService.GetErrorModel(contentItem, $"Unable to handle content type: {contentItem.ContentType}."));
      }


   }
}
