using System;
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
using Cfg.Net.Contracts;

namespace Module.Services {

   public class ReportService : IReportService {

      private readonly IHttpContextAccessor _contextAccessor;
      private readonly IContentManager _contentManager;
      private readonly IContentAliasManager _aliasManager;
      private readonly INotifier _notifier;
      private readonly IHtmlLocalizer<ReportService> H;
      private readonly IReportLoadService _reportLoadService;
      private readonly IReportRunService _reportRunService;
      private readonly IContentPermissionsService _contentPermissionsService;

      public ReportService(
         IContentManager contentManager,
         IContentAliasManager aliasManager,
         IReportLoadService reportLoadService,
         IReportRunService reportRunService,
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
         _reportLoadService = reportLoadService;
         _reportRunService = reportRunService;
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

      public Process LoadForExport(string arrangement, IPipelineLogger logger) {
         return _reportLoadService.LoadForExport(arrangement, logger);
      }

      public Process Load(ContentItem contentItem, string arrangement, IPipelineLogger logger, ISerializer serializer = null) {
         return _reportLoadService.Load(contentItem, arrangement, logger, serializer);
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {
         await _reportRunService.RunAsync(process, logger);
         return;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _contentPermissionsService.CanAccess(contentItem);
      }

   }
}
