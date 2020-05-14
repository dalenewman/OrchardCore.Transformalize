using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using Transformalize.Configuration;
using Module.ViewModels;
using Transformalize.Logging;
using Transformalize.Contracts;
using Etch.OrchardCore.ContentPermissions.Services;
using Module.Models;

namespace Module.Services {

   public class ReportService : IReportService {

      private readonly IHttpContextAccessor _contextAccessor;
      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly INotifier _notifier;
      private readonly IHtmlLocalizer<ReportService> H;
      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementRunService _runService;
      private readonly IContentPermissionsService _contentPermissionsService;

      public ReportService(
         IContentManager contentManager,
         IContentAliasManager aliasManager,
         IArrangementLoadService loadService,
         IArrangementRunService runService,
         INotifier notifier,
         IHttpContextAccessor contextAccessor,
         IHtmlLocalizer<ReportService> htmlLocalizer,
         IContentPermissionsService contentPermissionsService
      ) {
         _contentManager = contentManager;
         _aliasManager = aliasManager;
         _notifier = notifier;
         _contextAccessor = contextAccessor;
         H = htmlLocalizer;
         _loadService = loadService;
         _runService = runService;
         _contentPermissionsService = contentPermissionsService;
      }

      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         ContentItem contentItem = null;
         if (idOrAlias.Length == Common.IdLength) {
            contentItem = await _contentManager.GetAsync(idOrAlias);
         }
         if (contentItem == null) {
            var id = await _aliasManager.GetContentItemIdAsync("alias:" + idOrAlias);
            if (id != null) {
               contentItem = await _contentManager.GetAsync(id);
            }
         }
         return contentItem;
      }

      public bool IsMissingRequiredParameters(List<Parameter> parameters) {

         var hasRequiredParameters = true;
         foreach (var parameter in parameters.Where(p => p.Required)) {

            var value = _contextAccessor.HttpContext.Request.Query[parameter.Name].ToString();
            if (value != null && value != "*") {
               continue;
            }

            if (parameter.Sticky && parameter.Value != "*") {
               continue;
            }

            _notifier.Add(NotifyType.Warning, H["{0} is required. To continue, please choose a {0}.", parameter.Label]);
            if (hasRequiredParameters) {
               hasRequiredParameters = false;
            }
         }

         return !hasRequiredParameters;
      } 

      public ReportViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new ReportViewModel(new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(LogLevel.Error, null, message) } }, contentItem);
      }

      public Process LoadForExport(ContentItem contentItem, IPipelineLogger logger) {
         return _loadService.LoadForExport(contentItem, logger);
      }

      public Process LoadForReport(ContentItem contentItem, IPipelineLogger logger, string format = null) {
         return _loadService.LoadForReport(contentItem, logger, format);
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {
         await _runService.RunAsync(process, logger);
      }

      public bool CanAccess(ContentItem contentItem) {
         return _contentPermissionsService.CanAccess(contentItem);
      }

   }
}
