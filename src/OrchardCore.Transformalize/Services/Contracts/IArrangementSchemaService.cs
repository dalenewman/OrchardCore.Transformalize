using System.Threading.Tasks;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IArrangementSchemaService {
      Task<Process> GetSchemaAsync(Process process);
   }
}
