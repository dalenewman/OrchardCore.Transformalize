using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementRunService {
      Task RunAsync(Process process);
   }
}
