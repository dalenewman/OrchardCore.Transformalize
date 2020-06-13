using Module.Models;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ITaskService : IArrangementService, IArrangementRunService {
      Process LoadForTask(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null);
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }
}
