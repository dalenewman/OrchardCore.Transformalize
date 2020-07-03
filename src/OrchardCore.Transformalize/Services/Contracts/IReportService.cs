using TransformalizeModule.Models;
using System.Threading.Tasks;

namespace TransformalizeModule.Services.Contracts {
   public interface IReportService : IReportLoadService, IArrangementService, IArrangementRunService {
      Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request);
   }
}
