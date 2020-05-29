using Autofac;
using Module.Services.Contracts;
using StackExchange.Profiling;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {
   public class ArrangementRunService<T> : IArrangementRunService<T> {

      private readonly IContainer _container;

      public ArrangementRunService(
         IContainer container
      ) {
         _container = container;
      }

      public async Task RunAsync(Process process, CombinedLogger<T> logger) {

         IProcessController controller;

         using (MiniProfiler.Current.Step("Run.Prepare")) {
            controller = _container.CreateScope(process, logger).Resolve<IProcessController>();
         }

         using (MiniProfiler.Current.Step("Run.Execute")) {
            await controller.ExecuteAsync();
         }

         if (process.Errors().Any() || logger.Log.Any(l => l.LogLevel == LogLevel.Error)) {
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
