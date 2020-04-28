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
   public class CsvController : Controller {

      private readonly ISlugService _slugService;
      private readonly IReportService _reportService;

      public CsvController(
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

            var parameters = _reportService.GetParameters();

            var process = _reportService.Load(part.Arrangement.Arrangement, parameters, logger);

            if (process.Errors().Any()) {
               return Problem();
            }

            ConvertToExport(process, _slugService.Slugify(contentItem.As<TitlePart>().Title));

            _reportService.SetPageSize(process, parameters, 0, 0, process.Connections.First().Provider == "bogus" ? 100 : 0);

            if (parameters.ContainsKey("sort") && parameters["sort"] != null) {
               _reportService.AddSortToEntity(process.Entities.First(), parameters["sort"]);
            }

            await _reportService.RunAsync(process, logger);

            return new EmptyResult();

         }

         return Problem();
      }

      private void ConvertToExport(Process process, string fileName) {

         process.ReadOnly = true;

         var o = process.Output();

         Response.ContentType = "application/csv";
         o.Stream = true;
         o.Provider = "file";
         o.Delimiter = ",";
         o.TextQualifier = "\"";
         o.File = fileName + ".csv";

         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         // common
         foreach (var entity in process.Entities) {

            foreach (var field in entity.GetAllFields()) {
               if (field.System) {
                  field.Output = false;
               }
               field.Output = field.Output && field.Export == "defer" || field.Export == "true";
            }
         }
      }



   }
}
