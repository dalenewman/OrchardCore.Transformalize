using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using Module.Models;
using Module.Services.Contracts;
using Module.ViewModels;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Providers.Ado.Autofac;
using Transformalize.Providers.PostgreSql.Autofac;
using Transformalize.Providers.SqlServer.Autofac;

namespace Module.Controllers {
   public class ReportController : BaseController {
      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly ISortService _sortService;
      private readonly INotifier _notifier;
      public ReportController(IContentManager contentManager, IContentAliasManager aliasManager, IStringLocalizer<BaseController> stringLocalizer, IHtmlLocalizer<BaseController> htmlLocalizer, ISortService sortService, INotifier notifier) : base(stringLocalizer, htmlLocalizer) {
         _contentManager = contentManager;
         _aliasManager = aliasManager;
         _sortService = sortService;
         _notifier = notifier;
      }


      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {
         var contentItem = await _contentManager.GetAsync(contentItemId);

         if (contentItem == null) {
            var id = await _aliasManager.GetContentItemIdAsync("alias:" + contentItemId);
            if (id == null) {
               return NotFound();
            } else {
               contentItem = await _contentManager.GetAsync(id);
            }
         }

         var part = contentItem.As<TransformalizeArrangementPart>();
         var logger = new MemoryLogger(LogLevel.Info);

         if (part != null) {

            var parameters = GetParameters();
            GetStickyParameters(part.ContentItem.ContentItemId, parameters);

            var process = new ConfigurationContainer().CreateScope(part.Arrangement.Arrangement, logger, parameters).Resolve<Process>();

            if (process.Errors().Any()) {
               process.Log = logger.Log;
               return View("Error", new ReportViewModel(process, contentItem));
            }

            process.Mode = "report";
            process.ReadOnly = true;
            SetStickyParameters(contentItem.ContentItemId, process.Parameters);

            var sizes = new List<int>();
            sizes.AddRange(new int[] { 20, 50, 100 });  //todo: put in report controller content type
            var stickySize = GetStickyParameter(contentItem.ContentItemId, "size", () => sizes.Min());

            SetPageSize(process, parameters, sizes.Min(), stickySize, sizes.Max());

            if (parameters.ContainsKey("sort") && parameters["sort"] != null) {
               _sortService.AddSortToEntity(process.Entities.First(), parameters["sort"]);
            }

            if (IsMissingRequiredParameters(process.Parameters, _notifier)) {
               return View(new ReportViewModel(process, contentItem));
            }

            try {
               // todo: have these modules come from dependency injection
               new Container(new AdoProviderModule(), new SqlServerModule(), new PostgreSqlModule()).CreateScope(process, logger).Resolve<IProcessController>().Execute();
               if (process.Errors().Any()) {
                  process.Status = 500;
                  process.Message = "Error";
                  process.Log = logger.Log;
                  return View("Error", new ReportViewModel(process, contentItem));
               }
               process.Status = 200;
               process.Message = "Ok";
            } catch (System.Exception ex) {
               process.Status = 500;
               process.Message = ex.Message;
               process.Log.Add(new LogEntry(LogLevel.Error, null, ex.Message));
               process.Log.AddRange(logger.Log);
               return View("Error", new ReportViewModel(process, contentItem));
            }

            return View(new ReportViewModel(process, contentItem));

         }

         return new ContentResult() { ContentType = "text/plain", Content = string.Join("\r\n", logger.Log.Select(le => le.ToString())) };
      }


   }
}
