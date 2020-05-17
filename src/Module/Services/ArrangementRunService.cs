using Autofac;
using Module.Services.Contracts;
using StackExchange.Profiling;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Logging;

namespace Module.Services {
   public class ArrangementRunService : IArrangementRunService {

      private readonly IContainer _container;

      public ArrangementRunService(
         IContainer container
      ) {
         _container = container;
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {

         IProcessController controller;

         using (MiniProfiler.Current.Step("Run.Prepare")) {
            controller = _container.CreateScope(process, logger).Resolve<IProcessController>();
         }

         using (MiniProfiler.Current.Step("Run.Execute")) {
            await controller.ExecuteAsync();
         }

         if (process.Errors().Any() || logger is MemoryLogger m && m.Log.Any(l => l.LogLevel == LogLevel.Error)) {
            if (logger is MemoryLogger ml) {
               process.Log.AddRange(ml.Log);
            }
            process.Status = 500;
            process.Message = "Error";
         } else {
            process.Status = 200;
            process.Message = "Ok";
         }

         return;

      }
   }
}
