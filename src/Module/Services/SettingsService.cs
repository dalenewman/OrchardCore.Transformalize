using Esprima.Ast;
using Module.Models;
using Module.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;

namespace Module.Services {

   // Idea: If all processes were first loaded into facade, and manipulated, modify the actual process wouldn't be needed

   public class SettingsService : ISettingsService {

      public Process Process { get; set; }
      public Transformalize.ConfigurationFacade.Process ProcessFacade { get; set; }
      public TransformalizeSettings Settings { get; }

      public SettingsService(ISiteService siteService) {
         var result = siteService.GetSiteSettingsAsync().Result;
         Settings = result.As<TransformalizeSettings>();

         if (Settings.CommonArrangement == string.Empty) {
            Process = new Process();
            ProcessFacade = new Transformalize.ConfigurationFacade.Process();
         } else {
            Process = new Process(Settings.CommonArrangement);
            ProcessFacade = new Transformalize.ConfigurationFacade.Process(Settings.CommonArrangement);
         }

         foreach (var connection in Process.Connections) {
            Connections.Add(connection.Name, connection);
         }
         foreach (var connection in ProcessFacade.Connections) {
            ConnectionsFacade.Add(connection.Name, connection);
         }

         foreach (var map in Process.Maps) {
            Maps.Add(map.Name, map);
         }
         foreach (var map in ProcessFacade.Maps) {
            MapsFacade.Add(map.Name, map);
         }
      }

      public Dictionary<string, Connection> Connections { get; } = new Dictionary<string, Connection>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Connection> ConnectionsFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Connection>(StringComparer.OrdinalIgnoreCase);
      public Dictionary<string, Map> Maps { get; } = new Dictionary<string, Map>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Map> MapsFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Map>(StringComparer.OrdinalIgnoreCase);

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

      /// <summary>
      /// copies common arrangement settings into current process
      /// </summary>
      /// <param name="process">the transformalize report process</param>
      public void ApplyCommonSettings(Process process) {

         // common connections
         for (int i = 0; i < process.Connections.Count; i++) {
            var connection = process.Connections[i];
            if (Connections.ContainsKey(connection.Name) && connection.Provider == Transformalize.Constants.DefaultSetting) {
               var key = connection.Key;
               process.Connections[i] = Connections[connection.Name];
               process.Connections[i].Key = key;
            }
         }

         // common maps
         for (int i = 0; i < process.Maps.Count; i++) {
            var map = process.Maps[i];
            if (Maps.ContainsKey(map.Name) && !map.Items.Any() && map.Query == string.Empty) {
               process.Maps[i] = Maps[map.Name];
            }
         }
      }

      /// <summary>
      /// copies common arrangement settings into current process facade
      /// </summary>
      /// <param name="process">the transformalize report process</param>
      public void ApplyCommonSettings(Transformalize.ConfigurationFacade.Process process) {

         // common connections
         for (int i = 0; i < process.Connections.Count; i++) {
            var connection = process.Connections[i];
            if (connection.Provider == null && ConnectionsFacade.ContainsKey(connection.Name)) {
               process.Connections[i] = ConnectionsFacade[connection.Name];
            }
         }

         // common maps
         for (int i = 0; i < process.Maps.Count; i++) {
            var map = process.Maps[i];
            if (map.Query == null && !map.Items.Any() && MapsFacade.ContainsKey(map.Name) ) {
               process.Maps[i] = MapsFacade[map.Name];
            }
         }

      }
   }
}
