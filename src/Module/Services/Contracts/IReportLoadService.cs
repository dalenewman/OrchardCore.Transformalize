using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface IReportLoadService {
      Process Load(string arrangement, IDictionary<string, string> parameters, IPipelineLogger logger);
   }
}
