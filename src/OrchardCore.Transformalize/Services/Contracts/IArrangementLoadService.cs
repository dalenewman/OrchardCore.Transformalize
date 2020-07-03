using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IArrangementLoadService {
      Process LoadForReport(ContentItem contentItem, string format = null);
      Process LoadForMap(ContentItem contentItem);
      Process LoadForStream(ContentItem contentItem);
      Process LoadForTask(ContentItem contentItem, IDictionary<string,string> parameters = null, string format = null);
      Process LoadForBatch(ContentItem contentItem);
      Process LoadForMapStream(ContentItem contentItem);
      Process LoadForForm(ContentItem contentItem, IDictionary<string,string> parameters = null);
      Process LoadForSchema(ContentItem contentItem, string format = "xml");
   }
}
