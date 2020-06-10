using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface IFormService : IFormLoadService, IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }

}
