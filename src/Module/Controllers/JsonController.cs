using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Module.Models;
using Module.Services.Contracts;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Configuration;
using OrchardCore.Liquid;
using OrchardCore.Title.Models;

namespace Module.Controllers {
   public class JsonController : Controller {

      private readonly ISlugService _slugService;
      private readonly IReportService _reportService;

      public JsonController(
         IReportService reportService,
         ISlugService slugService
         ) {
         _slugService = slugService;
         _reportService = reportService;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

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

            ConvertToExport(process, _slugService.Slugify(contentItem.As<TitlePart>().Title));

            await _reportService.RunAsync(process, logger);

            return new EmptyResult();

         }

         return Problem();
      }

      private void ConvertToExport(Process process, string fileName) {

         var o = process.Output();
         o.Stream = true;
         o.Provider = "json";
         o.File = fileName + ".json";

         Response.ContentType = "application/json";
         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

      }

   }
}
