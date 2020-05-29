using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementRunService<T> {
      Task RunAsync(Process process, CombinedLogger<T> logger);
   }
}
