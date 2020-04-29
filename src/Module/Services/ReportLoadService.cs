using Autofac;
using Module.Services.Modifiers;
using Module.Services.Contracts;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using StackExchange.Profiling;

namespace Module.Services {
   public class ReportLoadService : IReportLoadService {

      public Process Load(string arrangement, IDictionary<string, string> parameters, IPipelineLogger logger) {

         Process process;

         using (MiniProfiler.Current.Step("Load")) {

            var container = new ConfigurationContainer();

            // configuration customizers
            container.AddCustomizer(new ReportParameterModifier());

            // external transforms register their short-hand here
            container.AddModule(new JintTransformModule());
            container.AddModule(new JsonTransformModule());
            container.AddModule(new HumanizeModule());

            process = container.CreateScope(arrangement, logger, parameters).Resolve<Process>();
         }

         process.Mode = "report";
         process.ReadOnly = true;

         return process;
      }
   }
}
