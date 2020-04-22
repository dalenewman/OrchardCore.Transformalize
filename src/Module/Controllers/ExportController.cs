using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Configuration;
using OrchardCore.Liquid;
using System;
using OrchardCore.Title.Models;

namespace Module.Controllers {
   public class ExportController : BaseController {

      private readonly ISortService _sortService;
      private readonly IReportLoadService _reportLoadService;
      private readonly IReportRunService _reportRunService;
      private readonly ISlugService _slugService;

      public ExportController(
         IContentManager contentManager,
         IContentAliasManager contentAliasManager,
         IReportLoadService reportLoadService,
         IReportRunService reportRunService,
         IStringLocalizer<BaseController> stringLocalizer,
         IHtmlLocalizer<BaseController> htmlLocalizer,
         ISortService sortService,
         ISlugService slugService
         ) : base(contentManager, contentAliasManager, stringLocalizer, htmlLocalizer) {
         _sortService = sortService;
         _reportLoadService = reportLoadService;
         _reportRunService = reportRunService;
         _slugService = slugService;
      }

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var contentItem = await GetByIdOrAliasAsync(contentItemId);
         if (contentItem == null) {
            return NotFound();
         }

         var part = contentItem.As<TransformalizeArrangementPart>();

         var logger = new MemoryLogger(LogLevel.Error);

         if (part != null) {

            var parameters = GetParameters();

            var process = _reportLoadService.Load(part.Arrangement.Arrangement, parameters, logger);

            if (process.Errors().Any()) {
               return Problem();
            }

            ConvertToExport(process, _slugService.Slugify(contentItem.As<TitlePart>().Title), Request.Query["output"].ToString());

            SetPageSize(process, parameters, 0, 0, process.Connections.First().Provider == "bogus" ? 100 : 0);

            if (parameters.ContainsKey("sort") && parameters["sort"] != null) {
               _sortService.AddSortToEntity(process.Entities.First(), parameters["sort"]);
            }

            await _reportRunService.RunAsync(process, logger);

            return new EmptyResult();

         }

         return Problem();
      }

      private void ConvertToExport(Process process, string fileName, string exportType) {

         process.ReadOnly = true;

         var o = process.Output();

         switch (exportType) {
            case "json":
               Response.ContentType = "application/json";
               o.Stream = true;
               o.Provider = "json";
               o.File = fileName + ".json";
               break;
            default: // csv
               Response.ContentType = "application/csv";
               o.Stream = true;
               o.Provider = "file";
               o.Delimiter = ",";
               o.TextQualifier = "\"";
               o.File = fileName + ".csv";
               break;
         }

         Response.Headers.Add("content-disposition", "attachment; filename=" + o.File);

         // common
         foreach (var entity in process.Entities) {

            foreach (var field in entity.GetAllFields()) {
               if (field.System || field.Alias == Common.BatchValueFieldName) {
                  field.Output = false;
               }
               field.Output = field.Output && field.Export == "defer" || field.Export == "true";
            }
         }
      }

      public void ConfineData(Process process, IDictionary<string, string> required) {

         foreach (var entity in process.Entities) {
            var all = entity.GetAllFields().ToArray();
            var dependencies = new HashSet<Field>();
            foreach (var field in all) {
               if (required.ContainsKey(field.Alias)) {
                  dependencies.Add(field);
                  field.Output = true;
                  if (!required[field.Alias].Equals(field.Alias, StringComparison.OrdinalIgnoreCase)) {
                     field.Alias = required[field.Alias];
                  }
               } else if (field.Property) {
                  dependencies.Add(field);
                  field.Output = true;
               }
            }
            // optimize download if it's not a manually written query
            if (entity.Query == string.Empty) {
               foreach (var field in entity.FindRequiredFields(dependencies, process.Maps)) {
                  dependencies.Add(field);
               }
               foreach (var unnecessary in all.Except(dependencies)) {
                  unnecessary.Input = false;
                  unnecessary.Output = false;
                  unnecessary.Transforms.Clear();
               }
            }
         }
      }

   }
}
