using Autofac;
using Microsoft.AspNetCore.Http;
using Module.Services.Contracts;
using Module.Transforms;
using StackExchange.Profiling;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Providers.Ado;
using Transformalize.Providers.Ado.Autofac;
using Transformalize.Providers.Bogus.Autofac;
using Transformalize.Providers.CsvHelper.Autofac;
using Transformalize.Providers.Elasticsearch.Autofac;
using Transformalize.Providers.Json.Autofac;
using Transformalize.Providers.MySql;
using Transformalize.Providers.MySql.Autofac;
using Transformalize.Providers.PostgreSql;
using Transformalize.Providers.PostgreSql.Autofac;
using Transformalize.Providers.Sqlite.Autofac;
using Transformalize.Providers.SQLite;
using Transformalize.Providers.SqlServer;
using Transformalize.Providers.SqlServer.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;
using Transformalize.Validate.Jint.Autofac;

namespace Module.Services {
   public class ArrangementRunService : IArrangementRunService {
      private readonly IHttpContextAccessor _httpContext;

      public ArrangementRunService(IHttpContextAccessor contextAccessor) {
         _httpContext = contextAccessor;
      }

      public async Task RunAsync(Process process, IPipelineLogger logger) {

         var profiler = MiniProfiler.Current;

         var container = new Container();

         IProcessController controller;

         using (profiler.Step("Run.Prepare")) {
            var providers = new HashSet<string>(process.Connections.Select(c => c.Provider));

            // providers

            if (providers.Contains("bogus")) { container.AddModule(new BogusModule()); }

            // ADO
            container.AddModule(new AdoProviderModule());
            if (providers.Contains("sqlserver")) { container.AddModule(new SqlServerModule() { ConnectionFactory = (c) => new ProfiledConnectionFactory(new SqlServerConnectionFactory(c)) }); }
            if (providers.Contains("postgresql")) { container.AddModule(new PostgreSqlModule() { ConnectionFactory = (c) => new ProfiledConnectionFactory(new PostgreSqlConnectionFactory(c)) }); }
            if (providers.Contains("sqlite")) { container.AddModule(new SqliteModule() { ConnectionFactory = (c) => new ProfiledConnectionFactory(new SqliteConnectionFactory(c)) }); }
            if (providers.Contains("mysql")) { container.AddModule(new MySqlModule() { ConnectionFactory = (c) => new ProfiledConnectionFactory(new MySqlConnectionFactory(c)) }); }

            if (providers.Contains("file")) { container.AddModule(new CsvHelperProviderModule(_httpContext.HttpContext.Response.Body)); }
            if (providers.Contains("json")) { container.AddModule(new JsonProviderModule(_httpContext.HttpContext.Response.Body)); }

            if (providers.Contains("elasticsearch")) { container.AddModule(new ElasticsearchModule()); }
            // solr
            // lucene

            // transforms
            container.AddTransform((c) => new UsernameTransform(_httpContext, c), new UsernameTransform().GetSignatures());

            // external transforms
            container.AddModule(new JintTransformModule());
            container.AddModule(new JsonTransformModule());
            container.AddModule(new HumanizeModule());

            // validators
            container.AddModule(new JintValidateModule());

            controller = container.CreateScope(process, logger).Resolve<IProcessController>();
         }

         using (profiler.Step("Run.Execute")) {
            await controller.ExecuteAsync();
         }

         if (process.Errors().Any() || logger is MemoryLogger m && m.Log.Any(l => l.LogLevel == LogLevel.Error)) {
            if (logger is MemoryLogger ml) {
               process.Log.AddRange(ml.Log);
            }
            process.Status = 500;
            process.Message = "Process has errors.";
         } else {
            process.Status = 200;
            process.Message = "Ok";
         }

         return;

      }
   }

   public class ProfiledConnectionFactory : IConnectionFactory {
      private readonly IConnectionFactory _original;
      public ProfiledConnectionFactory(IConnectionFactory original) {
         _original = original;
      }

      public AdoProvider AdoProvider => _original.AdoProvider;

      public string Terminator => _original.Terminator;

      public bool SupportsLimit => _original.SupportsLimit;

      public string Enclose(string name) {
         return _original.Enclose(name);
      }

      public IDbConnection GetConnection(string appName = null) {
         return new StackExchange.Profiling.Data.ProfiledDbConnection(_original.GetConnection(appName) as DbConnection, MiniProfiler.Current);
      }

      public string GetConnectionString(string appName = null) {
         return _original.GetConnectionString(appName);
      }

      public string SqlDataType(Field field) {
         return _original.SqlDataType(field);
      }
   }
}
