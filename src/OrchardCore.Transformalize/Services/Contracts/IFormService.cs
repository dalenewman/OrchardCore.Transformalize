using TransformalizeModule.Models;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using System.Collections.Generic;

namespace TransformalizeModule.Services.Contracts {
   public interface IFormService : IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeTaskPart>> ValidateTaskForm(TransformalizeRequest request);
      Process LoadForTaskForm(ContentItem contentItem, IDictionary<string, string> parameters = null);

      Task<TransformalizeResponse<TransformalizeFormPart>> ValidateForm(TransformalizeRequest request);
      Process LoadForForm(ContentItem contentItem, IDictionary<string, string> parameters = null);
   }

}
