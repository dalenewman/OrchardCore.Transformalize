using Autofac;
using Module.Services.Modifiers;
using Module.Services.Contracts;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Transforms.Jint.Autofac;

namespace Module.Services {
   public class ReportLoadService : IReportLoadService {

      public Process Load(string arrangement, IDictionary<string, string> parameters, IPipelineLogger logger) {
         var container = new ConfigurationContainer();
         container.AddCustomizer(new ReportParameterModifier());
         container.AddModule(new JintTransformModule());
         return container.CreateScope(arrangement, logger, parameters).Resolve<Process>();
      }
   }
}
