using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services.Contracts {
   public interface IReportRunService {
      Task RunAsync(Process process, IPipelineLogger logger);
   }
}
