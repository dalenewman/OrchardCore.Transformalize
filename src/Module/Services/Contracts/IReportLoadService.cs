using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IReportLoadService<T> {
      Process LoadForReport(ContentItem contentItem, CombinedLogger<T> logger, string format = null);
      Process LoadForExport(ContentItem contentItem, CombinedLogger<T> logger);
      Process LoadForBatch(ContentItem contentItem, CombinedLogger<T> logger);
   }
}
