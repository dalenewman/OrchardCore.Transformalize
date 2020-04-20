using Autofac;
using Module.Services.Contracts;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Providers.Ado.Autofac;
using Transformalize.Providers.PostgreSql.Autofac;
using Transformalize.Providers.SqlServer.Autofac;

namespace Module.Services {
   public class ReportRunService : IReportRunService {
      public void Run(Process process, IPipelineLogger logger) {

         new Container(
            new AdoProviderModule(),
            new SqlServerModule(),
            new PostgreSqlModule()
            ).CreateScope(process, logger).Resolve<IProcessController>().Execute();

      }
   }
}
