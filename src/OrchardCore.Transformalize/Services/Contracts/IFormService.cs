using TransformalizeModule.Models;
using System.Threading.Tasks;

namespace TransformalizeModule.Services.Contracts {
   public interface IFormService : IFormLoadService, IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }

}
