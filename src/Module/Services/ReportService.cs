using System.Collections.Generic;
using System.Threading.Tasks;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {

   public class ReportService<T> : IReportService<T> {

      private readonly IArrangementLoadService<T> _loadService;
      private readonly IArrangementRunService<T> _runService;
      private readonly IArrangementService _arrangementService;

      public ReportService(
         IArrangementLoadService<T> loadService,
         IArrangementRunService<T> runService,
         IArrangementService arrangementService
      ) {
         _loadService = loadService;
         _runService = runService;
         _arrangementService = arrangementService;      
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

      public Process LoadForExport(ContentItem contentItem, CombinedLogger<T> logger) {
         return _loadService.LoadForExport(contentItem, logger);
      }

      public Process LoadForReport(ContentItem contentItem, CombinedLogger<T> logger, string format = null) {
         return _loadService.LoadForReport(contentItem, logger, format);
      }

      public Process LoadForBatch(ContentItem contentItem, CombinedLogger<T> logger) {
         return _loadService.LoadForBatch(contentItem, logger);
      }

      public async Task RunAsync(Process process, CombinedLogger<T> logger) {
         await _runService.RunAsync(process, logger);
      }

   }
}
