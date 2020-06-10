using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IFormLoadService {
      Process LoadForForm(ContentItem contentItem, IDictionary<string, string> parameters = null);
   }
}
