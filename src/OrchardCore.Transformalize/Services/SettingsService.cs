using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using OrchardCore.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;
using StackExchange.Profiling;
using TransformalizeModule.Ext;

namespace TransformalizeModule.Services {

   /// <summary>
   /// This provides module-wide Transformalize settings.
   /// This is created once per request and currently nothing is cached.
   /// </summary>
   public class SettingsService : ISettingsService {

      public Process Process { get; set; }
      public Transformalize.ConfigurationFacade.Process ProcessFacade { get; set; }
      public TransformalizeSettings Settings { get; }
      public Dictionary<string, Parameter> Parameters { get; } = new Dictionary<string, Parameter>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Parameter> ParametersFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Parameter>(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, Map> Maps { get; } = new Dictionary<string, Map>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Map> MapsFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Map>(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, Transformalize.Configuration.Action> Actions { get; } = new Dictionary<string, Transformalize.Configuration.Action>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Action> ActionsFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Action>(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, Connection> Connections { get; } = new Dictionary<string, Connection>();
      private readonly Dictionary<string, Transformalize.ConfigurationFacade.Connection> ConnectionsFacade = new Dictionary<string, Transformalize.ConfigurationFacade.Connection>(StringComparer.OrdinalIgnoreCase);

      public SettingsService(ISiteService siteService) {

         using (MiniProfiler.Current.Step("Common Settings Setup")) {

            var result = siteService.GetSiteSettingsAsync().Result;
            Settings = result.As<TransformalizeSettings>();

            if (Settings.CommonArrangement == string.Empty) {
               Process = new Process();
               ProcessFacade = new Transformalize.ConfigurationFacade.Process();
            } else {
               Process = new Process(Settings.CommonArrangement);
               ProcessFacade = new Transformalize.ConfigurationFacade.Process(Settings.CommonArrangement);
            }

            // parameters
            foreach (var parameter in Process.Parameters) {
               Parameters.Add(parameter.Name, parameter);
            }
            foreach (var parameter in ProcessFacade.Parameters) {
               ParametersFacade.Add(parameter.Name, parameter);
            }

            // maps
            foreach (var map in Process.Maps) {
               Maps.Add(map.Name, map);
            }
            foreach (var map in ProcessFacade.Maps) {
               MapsFacade.Add(map.Name, map);
            }

            // actions
            foreach (var action in Process.Actions) {
               Actions.Add(action.Name, action);
            }
            foreach (var action in ProcessFacade.Actions) {
               ActionsFacade.Add(action.Name, action);
            }

            // connections
            foreach (var connection in Process.Connections) {
               Connections.Add(connection.Name, connection);
            }
            foreach (var connection in ProcessFacade.Connections) {
               ConnectionsFacade.Add(connection.Name, connection);
            }

         }

      }

      /// <summary>
      /// Get page sizes from module settings unless overridden by report settings
      /// </summary>
      /// <param name="part">the report part being displayed</param>
      /// <returns>a list of available page sizes</returns>
      public IEnumerable<int> GetPageSizes(TransformalizeReportPart part) {
         if (part.PageSizes.Enabled()) {
            if (part.PageSizes.OverrideDefaults()) {
               return part.PageSizes.SplitIntegers(',');
            } else {
               return Settings.DefaultPageSizesAsEnumerable();
            }
         } else {
            return Enumerable.Empty<int>();
         }
      }

      /// <summary>
      /// This method transfers module defined parameters, maps, connections, and actions into a process.
      /// This is used to consolidate such things for easy maintenance.
      /// </summary>
      /// <param name="process">the transformalize process</param>
      public void ApplyCommonSettings(Process process) {

         // common parameters
         for (int i = 0; i < process.Parameters.Count; i++) {
            var parameter = process.Parameters[i];
            if (Parameters.ContainsKey(parameter.Name) && string.IsNullOrEmpty(parameter.Value) && parameter.T == string.Empty && !parameter.Transforms.Any() && parameter.V == string.Empty && !parameter.Validators.Any()) {
               process.Parameters[0] = Parameters[parameter.Name];
            }
         }

         // common maps
         for (int i = 0; i < process.Maps.Count; i++) {
            var map = process.Maps[i];
            if (Maps.ContainsKey(map.Name) && !map.Items.Any() && map.Query == string.Empty) {
               process.Maps[i] = Maps[map.Name];
            }
         }

         // common actions
         for (int i = 0; i < process.Actions.Count; i++) {
            var action = process.Actions[i];
            if (action.Name != null && Actions.ContainsKey(action.Name) && action.Type == "internal") {
               var key = action.Key;
               process.Actions[i] = Actions[action.Name];
               process.Actions[i].Key = key;
            }
         }

         // common connections
         for (int i = 0; i < process.Connections.Count; i++) {
            var connection = process.Connections[i];
            if (Connections.ContainsKey(connection.Name) && connection.Provider == Transformalize.Constants.DefaultSetting) {
               var key = connection.Key;
               process.Connections[i] = Connections[connection.Name];
               process.Connections[i].Key = key;
            }
         }

      }

      /// <summary>
      /// This copies common arrangement settings into current process facade.
      /// All facade properties are strings without defaults, so they will be null if not provided.
      /// </summary>
      /// <param name="process">the transformalize report process</param>
      public void ApplyCommonSettings(Transformalize.ConfigurationFacade.Process process) {

         // common parameters
         for (int i = 0; i < process.Parameters.Count; i++) {
            var parameter = process.Parameters[i];
            if (parameter.Value == null && parameter.Name != null && ParametersFacade.ContainsKey(parameter.Name) && parameter.T == null && !parameter.Transforms.Any() && parameter.V == null && !parameter.Validators.Any()) {
               process.Parameters[0] = ParametersFacade[parameter.Name];
            }
         }

         // common maps
         for (int i = 0; i < process.Maps.Count; i++) {
            var map = process.Maps[i];
            if (map.Query == null && !map.Items.Any() && MapsFacade.ContainsKey(map.Name)) {
               process.Maps[i] = MapsFacade[map.Name];
            }
         }

         // common actions
         for (int i = 0; i < process.Actions.Count; i++) {
            var action = process.Actions[i];
            if (action.Type == null && action.Name != null && ActionsFacade.ContainsKey(action.Name)) {
               process.Actions[i] = ActionsFacade[action.Name];
            }
         }

         // common connections
         for (int i = 0; i < process.Connections.Count; i++) {
            var connection = process.Connections[i];
            if (connection.Provider == null && ConnectionsFacade.ContainsKey(connection.Name)) {
               process.Connections[i] = ConnectionsFacade[connection.Name];
            }
         }

      }
   }
}
