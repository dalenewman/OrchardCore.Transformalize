using System.Threading.Tasks;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IArrangementRunService {
      Task RunAsync(Process process);
   }
}
