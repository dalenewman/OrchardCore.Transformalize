using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {
   public class TaskService : ITaskService {

      private readonly IArrangementService _arrangementService;
      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementRunService _runService;

      public TaskService(IArrangementService arrangementService, IArrangementLoadService loadService, IArrangementRunService runService) {
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

      public Process LoadForTask(ContentItem contentItem, IPipelineLogger logger, string format = null) {
         return _loadService.LoadForTask(contentItem, logger, format);
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {
         await _runService.RunAsync(process, logger);
      }
   }
}
