using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IReportLoadService {
      Process LoadForReport(ContentItem contentItem, string format = null);
      Process LoadForExport(ContentItem contentItem);
      Process LoadForBatch(ContentItem contentItem);
   }
}
