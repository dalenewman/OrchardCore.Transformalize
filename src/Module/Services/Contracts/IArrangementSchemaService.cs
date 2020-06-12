using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementSchemaService {
      Task<Process> GetSchemaAsync(Process process);
   }
}
