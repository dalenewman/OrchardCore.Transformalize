using Autofac;
using Microsoft.AspNetCore.Http;
using Module.Services.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Providers.Ado.Autofac;
using Transformalize.Providers.Bogus.Autofac;
using Transformalize.Providers.CsvHelper.Autofac;
using Transformalize.Providers.PostgreSql.Autofac;
using Transformalize.Providers.SqlServer.Autofac;

namespace Module.Services {
   public class ReportRunService : IReportRunService {
      private readonly IHttpContextAccessor _contextAccessor;

      public ReportRunService(IHttpContextAccessor contextAccessor) {
         _contextAccessor = contextAccessor;
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {

         var providers = new HashSet<string>(process.Connections.Select(c => c.Provider));
         var ado = process.Connections.Any(c => c.Server != string.Empty || c.ConnectionString != string.Empty);
         var container = new Container();

         if (providers.Contains("bogus")) {
            container.AddModule(new BogusModule());
         }

         if (ado) {
            container.AddModule(new AdoProviderModule());
         }
         if (providers.Contains("sqlserver")) {
            container.AddModule(new SqlServerModule());
         }
         if (providers.Contains("postgresql")) {
            container.AddModule(new PostgreSqlModule());
         }
         if (providers.Contains("file")) {
            container.AddModule(new CsvHelperProviderModule(_contextAccessor.HttpContext.Response.Body));
         }

         await container.CreateScope(process, logger).Resolve<IProcessController>().ExecuteAsync();

         return;

      }
   }
}
