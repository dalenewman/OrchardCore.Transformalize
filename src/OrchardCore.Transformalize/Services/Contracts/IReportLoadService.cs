using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {
   public interface IReportLoadService {
      Process LoadForReport(ContentItem contentItem, string format = null);
      Process LoadForStream(ContentItem contentItem);
      Process LoadForBatch(ContentItem contentItem);
      Process LoadForMap(ContentItem contentItem);
      Process LoadForMapStream(ContentItem contentITem);
   }

}
