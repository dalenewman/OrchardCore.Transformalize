using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface IReportLoadService {
      Process Load(ContentItem contentItem, string arrangement, IPipelineLogger logger);
      Process LoadForExport(string arrangement, IPipelineLogger logger);
   }
}
