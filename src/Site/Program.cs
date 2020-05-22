using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchardCore.Logging;

namespace Site {
   public class Program {
      public static Task Main(string[] args) => BuildHost(args).RunAsync();

      public static IHost BuildHost(string[] args) {
         var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureWebHostDefaults(webBuilder => webBuilder
               .UseNLogWeb()
               .UseStartup<Startup>())
            .Build();

         return host;
      }
   }
}
