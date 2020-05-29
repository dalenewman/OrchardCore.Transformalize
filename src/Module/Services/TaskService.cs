using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {
   public class TaskService<T> : ITaskService<T> {

      private readonly IArrangementService _arrangementService;
      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;

      public TaskService(IArrangementService arrangementService, IArrangementLoadService<T> loadService, IArrangementRunService<T> runService) {
         _arrangementService = arrangementService;
         _loadService = loadService;
         _runService = runService;
      }

      public bool CanAccess(ContentItem contentItem) {
         return _arrangementService.CanAccess(contentItem);
      }

      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public bool IsMissingRequiredParameters(List<Parameter> parameters) {
         return _arrangementService.IsMissingRequiredParameters(parameters);
      }

      public Process LoadForTask(ContentItem contentItem, CombinedLogger<T> logger, IDictionary<string,string> parameters = null, string format = null) {
         return _loadService.LoadForTask(contentItem, logger, parameters, format);
      }

      public async Task RunAsync(Process process, CombinedLogger<T> logger) {
         await _runService.RunAsync(process, logger);
      }
   }
}
