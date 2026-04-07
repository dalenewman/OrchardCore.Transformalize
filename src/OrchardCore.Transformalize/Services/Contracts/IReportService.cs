using TransformalizeModule.Models;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {
   public interface IReportService : IArrangementService, IArrangementStreamService {
      Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request);

      Task<Process> LoadForReportAsync(ContentItem contentItem, string format = null);
      Task<Process> LoadForStreamAsync(ContentItem contentItem);
      Task<Process> LoadForBatchAsync(ContentItem contentItem);
      Task<Process> LoadForMapAsync(ContentItem contentItem);
      Task<Process> LoadForMapStreamAsync(ContentItem contentItem);
      Task<Process> LoadForChartAsync(ContentItem contentItem);
   }
}
