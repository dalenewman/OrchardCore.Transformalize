using Module.Models;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface ITaskService<T> : ITaskLoadService<T>, IArrangementService, IArrangementRunService<T> {
      Task<TaskComponents> Validate(ValidateRequest request);
   }

}
