using Module.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface ITaskService<T> : ITaskLoadService<T>, IArrangementService, IArrangementRunService<T> {
      Task<TaskComponents> Validate(string contentItemId, bool checkAccess, IDictionary<string,string> internalParameters = null);
   }

}
