using Autofac;
using Module.Services.Contracts;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Contracts;
using StackExchange.Profiling;
using OrchardCore.ContentManagement;
using System.Linq;
using System;
using Cfg.Net.Contracts;
using Module.Models;
using Cfg.Net.Serializers;
using Transformalize.Logging;
using Transformalize.Impl;

namespace Module.Services {
   public class ArrangementLoadService : IArrangementLoadService {

      private readonly IStickyParameterService _stickyParameterService;
      private readonly IDictionary<string, string> _parameters;
      private readonly ISortService _sortService;
      private readonly ISettingsService _settings;
      private readonly IConfigurationContainer _configurationContainer;
      private readonly CombinedLogger<ArrangementLoadService> _logger;

      public ArrangementLoadService(
         IParameterService parameterService,
         IStickyParameterService stickyParameterService,
         ISortService sortService,
         ISettingsService settings,
         IConfigurationContainer configurationContainer,
         CombinedLogger<ArrangementLoadService> logger
      ) {
         _stickyParameterService = stickyParameterService;
         _parameters = parameterService.GetParameters();
         _sortService = sortService;
         _settings = settings;
         _logger = logger;
         _configurationContainer = configurationContainer;
      }

      public Process LoadForExport(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForExport can't load {contentItem.ContentType}.") } };
         }

         var process = LoadInternal(part.Arrangement.Arrangement);

         process.Mode = "report";
         process.ReadOnly = true;

         if (part.PageSizes.Enabled()) {
            var size = process.Connections.First().Provider == "bogus" ? _settings.GetPageSizes(part).Max() : 0;
            EnforcePageSize(process, _parameters, size, size, size);
         }

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(process.Entities.First(), _parameters["sort"]);
         }

         // modify entities for output/export differences
         foreach (var entity in process.Entities) {
            foreach (var field in entity.GetAllFields()) {
               if (field.System) {
                  field.Output = false;
               }
               field.Output = field.Output && field.Export == "defer" || field.Export == "true";
            }
         }

         // disable actions
         foreach (var action in process.Actions) {
            action.Before = false;
            action.After = false;
         }

         return process;
      }

      public Process LoadForReport(ContentItem contentItem, string format = null) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForReport can't load {contentItem.ContentType}.") } };
         }

         _stickyParameterService.GetStickyParameters(contentItem.ContentItemId, _parameters);

         var process = LoadInternal(part.Arrangement.Arrangement, null, format == "json" ? new JsonSerializer() : null);

         process.Mode = "report";
         process.ReadOnly = true;

         _stickyParameterService.SetStickyParameters(contentItem.ContentItemId, process.Parameters);

         if (part.PageSizes.Enabled()) {
            var pageSizes = _settings.GetPageSizes(part);

            var stickySize = _stickyParameterService.GetStickyParameter(contentItem.ContentItemId, "size", () => pageSizes.Min());
            EnforcePageSize(process, _parameters, pageSizes.Min(), stickySize, pageSizes.Max());

            // modify connections to buffer (load page completely before processing)
            foreach (var connection in process.Connections) {
               connection.Buffer = true;
            }
         }

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(process.Entities.First(), _parameters["sort"]);
         }

         // disable internal actions
         foreach (var action in process.Actions.Where(a=>a.Type == "internal")) {
            action.Before = false;
            action.After = false;
         }

         // special handling of bulk action value field
         if (part.BulkActions.Value && process.TryGetField(part.BulkActionValueField.Text, out Field bulkActionValueField)) {
            bulkActionValueField.Output = true;
            bulkActionValueField.Export = "false";
         }

         return process;
      }

      public Process LoadForBatch(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForBatch can't load {contentItem.ContentType}.") } };
         }

         var process = LoadInternal(part.Arrangement.Arrangement);

         process.Mode = "report";
         process.ReadOnly = true;

         // disable actions
         foreach (var action in process.Actions) {
            action.Before = false;
            action.After = false;
         }

         // all we need is the batch value
         var requiredFields = new Dictionary<string, string>() {
            { part.BulkActionValueField.Text, part.BulkActionValueField.Text }
         };
         ConfineData(process, requiredFields);

         return process;
      }

      public Process LoadForTask(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null) {

         Process process;

         if (!TryGetTaskPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForTask can't load {contentItem.ContentType}.") } };
         }

         process = LoadInternal(part.Arrangement.Arrangement, parameters, format == "json" ? new JsonSerializer() : null);

         return process;
      }

      public Process LoadForSchema(ContentItem contentItem, string format = null) {

         Process process;
         string arrangement;

         if (TryGetTaskPart(contentItem, out var taskPart)) {
            arrangement = taskPart.Arrangement.Arrangement;
         } else if(TryGetReportPart(contentItem, out var reportPart)) {
            arrangement = reportPart.Arrangement.Arrangement;
         } else {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForSchema can't load {contentItem.DisplayText}.") } };
         }

         process = LoadInternal(arrangement, null, format == "json" ? new JsonSerializer() : null);

         return process;
      }

      public Process LoadForForm(ContentItem contentItem, IDictionary<string, string> parameters = null) {

         Process process;

         if (!TryGetTaskPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForTask can't load {contentItem.ContentType}.") } };
         }

         process = LoadInternal(part.Arrangement.Arrangement, parameters);

         // switch postback auto to true or false
         foreach (var parameter in process.Parameters.Where(p => p.Prompt && p.PostBack == "auto")) {

            parameter.PostBack = parameter.Validators.Any() ? "true" : "false";

            if (parameter.Map == string.Empty)
               continue;

            var map = process.Maps.FirstOrDefault(m => m.Name == parameter.Map);
            if (map == null)
               continue;

            if (!map.Query.Contains("@"))
               continue;

            // it is possible for this map to affect other field's post-back setting
            foreach (var p in new ParameterFinder().Find(map.Query).Distinct()) {
               var parameterField = process.Parameters.FirstOrDefault(f => f.Name == p);
               if (parameterField != null) {
                  parameterField.PostBack = "true";
               }
            }
         }

         return process;
      }

      private Process LoadInternal(string arrangement, IDictionary<string, string> parameters = null, ISerializer serializer = null) {

         Process process;

         if (parameters != null) {
            foreach (var kv in parameters) {
               _parameters[kv.Key] = kv.Value;
            }
         }

         using (MiniProfiler.Current.Step("Load")) {
            _configurationContainer.Serializer = serializer;
            process = _configurationContainer.CreateScope(arrangement, _logger, _parameters).Resolve<Process>();
         }

         ApplyCommonSettings(process);

         if (process.Errors().Any() || process.Log.Any(l => l.LogLevel == LogLevel.Error)) {
            process.Status = 500;
            process.Message = "Process has errors.";
         } else {
            process.Status = 200;
            process.Message = "Ok";
         }

         return process;
      }

      /// <summary>
      /// don't let the users put size=100000000 on the url
      /// </summary>
      /// <param name="process">the transformalize report process</param>
      /// <param name="parameters">incoming parameters</param>
      /// <param name="min">the minimum allowed page size</param>
      /// <param name="chosen">the page size selected by the user</param>
      /// <param name="max">the maximum allowed page size</param>
      private void EnforcePageSize(Process process, IDictionary<string, string> parameters, int min, int chosen, int max) {

         foreach (var entity in process.Entities) {
            // parse out a page number
            int page;
            if (parameters.ContainsKey("page")) {
               if (!int.TryParse(parameters["page"], out page)) {
                  page = 1;
               }
            } else {
               page = 1;
            }

            entity.Page = page;

            var size = chosen;
            if (parameters.ContainsKey("size")) {
               int.TryParse(parameters["size"], out size);
            }

            if (size == 0 && min > 0) {
               size = min;
            }
            entity.Size = max > 0 && size > max ? max : size;

         }

      }

      /// <summary>
      /// copies common arrangement settings into current process
      /// </summary>
      /// <param name="process">the transformalize report process</param>
      private void ApplyCommonSettings(Process process) {

         for (int i = 0; i < process.Connections.Count; i++) {
            var connection = process.Connections[i];
            if (connection.Provider == Transformalize.Constants.DefaultSetting && _settings.Connections.ContainsKey(connection.Name)) {
               var key = connection.Key;
               process.Connections[i] = _settings.Connections[connection.Name];
               process.Connections[i].Key = key;
            }
         }
      }

      /// <summary>
      /// removes unnecessary stuff from the main report for batching, geo-json, etc.
      /// </summary>
      /// <param name="process"></param>
      /// <param name="required"></param>
      private void ConfineData(Process process, IDictionary<string, string> required) {

         foreach (var entity in process.Entities) {
            var all = entity.GetAllFields().ToArray();
            var dependencies = new HashSet<Field>();
            foreach (var field in all) {
               if (required.ContainsKey(field.Alias)) {
                  dependencies.Add(field);
                  field.Output = true;
                  if (!required[field.Alias].Equals(field.Alias, StringComparison.OrdinalIgnoreCase)) {
                     field.Alias = required[field.Alias];
                  }
               } else if (field.Property) {
                  dependencies.Add(field);
                  field.Output = true;
               }
            }
            // optimize download if it's not a manually written query
            if (entity.Query == string.Empty) {
               foreach (var field in entity.FindRequiredFields(dependencies, process.Maps)) {
                  dependencies.Add(field);
               }
               foreach (var unnecessary in all.Except(dependencies)) {
                  unnecessary.Input = false;
                  unnecessary.Output = false;
                  unnecessary.Transforms.Clear();
               }
            }
         }
      }

      private bool TryGetReportPart(ContentItem contentItem, out TransformalizeReportPart part) {
         part = contentItem?.As<TransformalizeReportPart>();
         return part != null;
      }

      private bool TryGetTaskPart(ContentItem contentItem, out TransformalizeTaskPart part) {
         part = contentItem?.As<TransformalizeTaskPart>();
         return part != null;
      }

   }
}
