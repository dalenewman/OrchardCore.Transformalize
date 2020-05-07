using Cfg.Net.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface IReportLoadService {
      Process Load(ContentItem contentItem, string arrangement, IPipelineLogger logger, ISerializer serializer = null);
      Process LoadForExport(string arrangement, IPipelineLogger logger);
   }
}
