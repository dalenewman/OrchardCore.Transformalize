using System.Collections.Generic;
using System.Threading.Tasks;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {

   public class ReportService : IReportService {

      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementRunService _runService;
      private readonly IArrangementService _arrangementService;

      public ReportService(
         IArrangementLoadService loadService,
         IArrangementRunService runService,
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

      public Process LoadForExport(ContentItem contentItem, IPipelineLogger logger) {
         return _loadService.LoadForExport(contentItem, logger);
      }

      public Process LoadForReport(ContentItem contentItem, IPipelineLogger logger, string format = null) {
         return _loadService.LoadForReport(contentItem, logger, format);
      }

      public Process LoadForBatch(ContentItem contentItem, IPipelineLogger logger) {
         return _loadService.LoadForBatch(contentItem, logger);
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {
         await _runService.RunAsync(process, logger);
      }

   }
}
