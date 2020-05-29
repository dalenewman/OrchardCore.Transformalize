using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementLoadService<T> {
      Process LoadForReport(ContentItem contentItem, CombinedLogger<T> logger, string format = null);
      Process LoadForExport(ContentItem contentItem, CombinedLogger<T> logger);
      Process LoadForTask(ContentItem contentItem, CombinedLogger<T> logger, IDictionary<string,string> parameters = null, string format = null);
      Process LoadForBatch(ContentItem contentItem, CombinedLogger<T> logger);
   }
}
