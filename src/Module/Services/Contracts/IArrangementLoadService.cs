using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface IArrangementLoadService {
      Process LoadForReport(ContentItem contentItem, IPipelineLogger logger, string format = null);
      Process LoadForExport(ContentItem contentItem, IPipelineLogger logger);
      Process LoadForTask(ContentItem contentItem, IPipelineLogger logger, string format = null);
   }
}
