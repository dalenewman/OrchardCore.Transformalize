using OrchardCore.ContentManagement;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface ITaskLoadService { 
      Process LoadForTask(ContentItem contentItem, IPipelineLogger logger, IDictionary<string,string> parameters = null, string format = null); 
   }


}
