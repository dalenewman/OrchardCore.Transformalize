using TransformalizeModule.Models;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {
   public interface ITaskService : IArrangementService, IArrangementRunService {
      Task<Process> LoadForTaskAsync(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null);
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }
}
