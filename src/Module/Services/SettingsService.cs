using Esprima.Ast;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services {
   public class SettingsService : ISettingsService {

      public Process Process { get; set; }
      public TransformalizeSettings TransformalizeSettings { get; }

      public SettingsService(ISiteService siteService) {
         var result = siteService.GetSiteSettingsAsync().Result;
         TransformalizeSettings = result.As<TransformalizeSettings>();
         Process = TransformalizeSettings.CommonArrangement == string.Empty ? new Process() : new Process(TransformalizeSettings.CommonArrangement);
         foreach (var connection in Process.Connections) {
            Connections.Add(connection.Name, connection);
         }
      }

      public Dictionary<string, Connection> Connections { get; } = new Dictionary<string, Connection>();

   }
}
