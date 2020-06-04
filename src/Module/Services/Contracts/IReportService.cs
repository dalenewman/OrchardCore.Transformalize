using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface IReportService<T> : IReportLoadService<T>, IArrangementService, IArrangementRunService<T> {
      Task<ReportComponents> Validate(string contentItemId);
   }
}
