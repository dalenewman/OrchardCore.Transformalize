using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ITaskLoadService { 
      Process LoadForTask(ContentItem contentItem, IDictionary<string,string> parameters = null, string format = null); 
   }
}
