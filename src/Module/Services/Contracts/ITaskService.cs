using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface ITaskService<T> : ITaskLoadService<T>, IArrangementService<T>, IArrangementRunService<T> {
      Task<TransformalizeResponse<TransformalizeTaskPart>> Validate(TransformalizeRequest request);
   }

}
