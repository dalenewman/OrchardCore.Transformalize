using OrchardCore.TransformalizeModule.Models;
using System.Threading.Tasks;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IFormService : IFormLoadService, IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }

}
