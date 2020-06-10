using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementLoadService {
      Process LoadForReport(ContentItem contentItem, string format = null);
      Process LoadForExport(ContentItem contentItem);
      Process LoadForTask(ContentItem contentItem, IDictionary<string,string> parameters = null, string format = null);
      Process LoadForBatch(ContentItem contentItem);
      Process LoadForForm(ContentItem contentItem, IDictionary<string,string> parameters = null);
   }
}
