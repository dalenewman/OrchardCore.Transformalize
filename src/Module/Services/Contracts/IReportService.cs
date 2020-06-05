using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface IReportService<T> : IReportLoadService<T>, IArrangementService<T>, IArrangementRunService<T> {
      Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request);
   }
}
