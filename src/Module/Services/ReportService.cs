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
      private readonly ISortService _sortService;

      public ReportService(
         IContentManager contentManager,
         IContentAliasManager aliasManager,
         IReportLoadService reportLoadService,
         IReportRunService reportRunService,
         INotifier notifier,
         IHttpContextAccessor contextAccessor,
         IHtmlLocalizer<ReportService> htmlLocalizer,
         IContentPermissionsService contentPermissionsService,
         ISortService sortService
      ) {
         _contentManager = contentManager;
         _aliasManager = aliasManager;
         _notifier = notifier;
         _contextAccessor = contextAccessor;
         H = htmlLocalizer;
         _reportLoadService = reportLoadService;
         _reportRunService = reportRunService;
         _contentPermissionsService = contentPermissionsService;
         _sortService = sortService;
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

      public IDictionary<string, string> GetParameters() {

         var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         if (_contextAccessor.HttpContext.Request != null) {
            if (_contextAccessor.HttpContext.Request.QueryString != null) {
               foreach (var key in _contextAccessor.HttpContext.Request.Query.Keys) {
                  parameters[key] = _contextAccessor.HttpContext.Request.Query[key].ToString();
               }
            }
            if (_contextAccessor.HttpContext.Request.HasFormContentType) {
               foreach (var key in _contextAccessor.HttpContext.Request.Form.Keys) {
                  if (!_contextAccessor.HttpContext.Request.Form[key].ToString().Equals("__requestverificationtoken", StringComparison.OrdinalIgnoreCase)) {
                     parameters[key] = _contextAccessor.HttpContext.Request.Form[key];
                  }
               }
            }
         }

         return parameters;
      }

      public void SetPageSize(Process process, IDictionary<string, string> parameters, int min, int stickySize, int max) {

         foreach (var entity in process.Entities) {
            if (entity.Page <= 0) {
               continue;  // This entity isn't intended to be paged
            }

            // parse out a page number
            int page;
            if (parameters.ContainsKey("page")) {
               if (!int.TryParse(parameters["page"], out page)) {
                  page = 1;
               }
            } else {
               page = 1;
            }

            entity.Page = page;

            var size = stickySize;
            if (parameters.ContainsKey("size")) {
               int.TryParse(parameters["size"], out size);
            }

            if (size == 0 && min > 0) {
               size = min;
            }
            entity.Size = max > 0 && size > max ? max : size;

         }

      }

      public ReportViewModel GetErrorModel(ContentItem contentItem, string message) {
         return new ReportViewModel(new Process() { Name = "Error", Log = new List<LogEntry>(1) { new LogEntry(LogLevel.Error, null, message) } }, contentItem);
      }

      public Process Load(string arrangement, IDictionary<string, string> parameters, IPipelineLogger logger) {
         return _reportLoadService.Load(arrangement, parameters, logger);
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {
         await _reportRunService.RunAsync(process, logger);
         return;
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

      public bool CanAccess(ContentItem contentItem) {
         return _contentPermissionsService.CanAccess(contentItem);
      }

      public Direction Sort(int fieldNumber, string expression) {
         return _sortService.Sort(fieldNumber, expression);
      }

      public void AddSortToEntity(Entity entity, string expression) {
         _sortService.AddSortToEntity(entity, expression);
      }
   }
}
