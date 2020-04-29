using Autofac;
using Microsoft.AspNetCore.Http;
using Module.Services.Contracts;
using StackExchange.Profiling;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;

using Transformalize.Providers.Ado.Autofac;
using Transformalize.Providers.Bogus.Autofac;
using Transformalize.Providers.CsvHelper.Autofac;
using Transformalize.Providers.Elasticsearch.Autofac;
using Transformalize.Providers.Json.Autofac;
using Transformalize.Providers.MySql.Autofac;
using Transformalize.Providers.PostgreSql.Autofac;
using Transformalize.Providers.Sqlite.Autofac;
using Transformalize.Providers.SqlServer.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;

namespace Module.Services {
   public class ReportRunService : IReportRunService {
      private readonly IHttpContextAccessor _contextAccessor;

      public ReportRunService(IHttpContextAccessor contextAccessor) {
         _contextAccessor = contextAccessor;
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {

         var profiler = MiniProfiler.Current;

         var container = new Container();

         IProcessController controller;

         using (profiler.Step("Run.Prepare")) {
            var providers = new HashSet<string>(process.Connections.Select(c => c.Provider));

            // providers
            container.AddModule(new AdoProviderModule());
            if (providers.Contains("bogus")) { container.AddModule(new BogusModule()); }
            if (providers.Contains("sqlserver")) { container.AddModule(new SqlServerModule()); }
            if (providers.Contains("postgresql")) { container.AddModule(new PostgreSqlModule()); }
            if (providers.Contains("sqlite")) { container.AddModule(new SqliteModule()); }
            if (providers.Contains("mysql")) { container.AddModule(new MySqlModule()); }
            if (providers.Contains("file")) { container.AddModule(new CsvHelperProviderModule(_contextAccessor.HttpContext.Response.Body)); }
            if (providers.Contains("elasticsearch")) { container.AddModule(new ElasticsearchModule()); }
            if (providers.Contains("json")) { container.AddModule(new JsonProviderModule(_contextAccessor.HttpContext.Response.Body)); }

            // transforms
            container.AddModule(new JintTransformModule());
            container.AddModule(new JsonTransformModule());
            container.AddModule(new HumanizeModule());

            controller = container.CreateScope(process, logger).Resolve<IProcessController>();
         }

         using (profiler.Step("Run.Execute")) {
            await controller.ExecuteAsync();
         }

         return;

      }
   }
}
