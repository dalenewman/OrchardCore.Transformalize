using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface IReportService : IReportLoadService, IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request);
   }
}
