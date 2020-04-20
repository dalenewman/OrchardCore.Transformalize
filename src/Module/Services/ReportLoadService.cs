using Autofac;
using Module.Services.ArrangementModifiers;
using Module.Services.Contracts;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;

namespace Module.Services {
   public class ReportLoadService : IReportLoadService {

      public Process Load(string arrangement, IDictionary<string, string> parameters, IPipelineLogger logger) {
         var container = new ConfigurationContainer();
         container.AddCustomizer(new ReportParameterModifier());
         return container.CreateScope(arrangement, logger, parameters).Resolve<Process>();
      }
   }
}
