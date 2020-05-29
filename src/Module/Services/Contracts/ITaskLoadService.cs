using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ITaskLoadService<T> { 
      Process LoadForTask(ContentItem contentItem, CombinedLogger<T> logger, IDictionary<string,string> parameters = null, string format = null); 
   }
}
