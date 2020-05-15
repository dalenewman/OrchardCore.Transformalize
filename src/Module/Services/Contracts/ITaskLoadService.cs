using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface ITaskLoadService { 
      Process LoadForTask(ContentItem contentItem, IPipelineLogger logger, string format = null); 
   }


}
