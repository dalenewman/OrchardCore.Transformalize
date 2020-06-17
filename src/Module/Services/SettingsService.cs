using Esprima.Ast;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;

namespace Module.Services {
   public class SettingsService : ISettingsService {

      public Process Process { get; set; }
      public TransformalizeSettings Settings { get; }

      public SettingsService(ISiteService siteService) {
         var result = siteService.GetSiteSettingsAsync().Result;
         Settings = result.As<TransformalizeSettings>();
         Process = Settings.CommonArrangement == string.Empty ? new Process() : new Process(Settings.CommonArrangement);
         foreach (var connection in Process.Connections) {
            Connections.Add(connection.Name, connection);
         }
         foreach (var map in Process.Maps) {
            Maps.Add(map.Name, map);
         }
      }

      public Dictionary<string, Connection> Connections { get; } = new Dictionary<string, Connection>();
      public Dictionary<string, Map> Maps { get; } = new Dictionary<string, Map>();

      public IEnumerable<int> GetPageSizes(TransformalizeReportPart part) {
         if (part.PageSizes.Enabled()) {
            if (part.PageSizes.OverrideDefaults()) {
               return part.PageSizes.AsEnumerable();
            } else {
               return Settings.DefaultPageSizesAsEnumerable();
            }
         } else {
            return Enumerable.Empty<int>();
         }
      }

   }
}
