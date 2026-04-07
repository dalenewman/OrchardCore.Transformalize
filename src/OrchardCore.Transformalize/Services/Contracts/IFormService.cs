using TransformalizeModule.Models;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {

   public interface IFormService : IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeTaskPart>> ValidateParameters(TransformalizeRequest request);
      Task<Process> LoadForParametersAsync(ContentItem contentItem, IDictionary<string, string> parameters = null);

      Task<TransformalizeResponse<TransformalizeFormPart>> ValidateForm(TransformalizeRequest request);
      Task<Process> LoadForFormAsync(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null);
   }

}
