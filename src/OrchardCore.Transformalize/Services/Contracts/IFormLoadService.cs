using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IFormLoadService {
      Process LoadForForm(ContentItem contentItem, IDictionary<string, string> parameters = null);
   }
}
