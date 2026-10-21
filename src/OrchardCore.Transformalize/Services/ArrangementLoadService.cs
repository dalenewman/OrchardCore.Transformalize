using Autofac;
using TransformalizeModule.Services.Contracts;
using Transformalize.Configuration;
using Transformalize.Contracts;
using StackExchange.Profiling;
using OrchardCore.ContentManagement;
using Cfg.Net.Contracts;
using TransformalizeModule.Models;
using Cfg.Net.Serializers;
using Transformalize.Logging;
using Transformalize.Impl;
using TransformalizeModule.Ext;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.Environment.Cache;

namespace TransformalizeModule.Services {
   public class ArrangementLoadService : IArrangementLoadService {

      private readonly IDictionary<string, string> _parameters;
      private readonly ISortService _sortService;
      private readonly ISettingsService _settings;
      private readonly IConfigurationContainer _configurationContainer;
      private readonly CombinedLogger<ArrangementLoadService> _logger;
      private readonly IFileService _fileService;
      private readonly ICustomFileStore _formFileStore;
      private readonly IMemoryCache _memoryCache;
      private readonly ISignal _signal;
      private static readonly HashSet<string> _fileBasedProviders = new HashSet<string>() { "file", "excel" };

      public ArrangementLoadService(
         ICustomFileStore formFileStore,
         IParameterService parameterService,
         ISortService sortService,
         ISettingsService settings,
         IConfigurationContainer configurationContainer,
         IFileService fileService,
         CombinedLogger<ArrangementLoadService> logger,
         IMemoryCache memoryCache,
         ISignal signal
      ) {
         _formFileStore = formFileStore;
         _parameters = parameterService.GetParameters();
         _sortService = sortService;
         _settings = settings;
         _logger = logger;
         _fileService = fileService;
         _configurationContainer = configurationContainer;
         _memoryCache = memoryCache;
         _signal = signal;
      }

      public async Task<Process> LoadForStreamAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForStream can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part);

         process.Mode = "report";
         process.ReadOnly = true;

         AddSrc(process);

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(part, process.Entities.First(), _parameters["sort"]);
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

      public async Task<Process> LoadForReportAsync(ContentItem contentItem, string format = null) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForReport can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part, null, format == "json" ? new JsonSerializer() : null);

         process.Mode = "report";
         process.ReadOnly = true;


         var paging = ReportPaging.For(part, _settings.Settings, "report");
         if (paging.FetchPaged) {
            EnforcePageSize(process, _parameters, paging);

            // modify connections to buffer (load page completely before processing)
            foreach (var connection in process.Connections) {
               connection.Buffer = true;
            }
         }

         AddSrc(process);

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(part, process.Entities.First(), _parameters["sort"]);
         }

         // disable internal actions
         foreach (var action in process.Actions.Where(a => a.Type == "internal")) {
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

      public async Task<Process> LoadForMapAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForMap can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part, null);

         process.Mode = "map";
         process.ReadOnly = true;

         // TODO: Paging and Sorting may be handled differently for map view
         var paging = ReportPaging.For(part, _settings.Settings, "map");
         if (paging.FetchPaged) EnforcePageSize(process, _parameters, paging);

         AddSrc(process);

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(part, process.Entities.First(), _parameters["sort"]);
         }

         // disable internal actions
         foreach (var action in process.Actions.Where(a => a.Type == "internal")) {
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

      public async Task<Process> LoadForChartAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForChart can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part, null);

         process.Mode = "chart";
         process.ReadOnly = true;

         // maps (facets) are populated during parameter processing, not from entity rows
         foreach (var entity in process.Entities) {
            entity.Page = 1;
            entity.Size = 1;
         }

         AddSrc(process);

         // disable internal actions
         foreach (var action in process.Actions.Where(a => a.Type == "internal")) {
            action.Before = false;
            action.After = false;
         }

         return process;
      }

      public async Task<Process> LoadForCalendarAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForCalendar can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part, null);
         if (process.Status != 200 || !ValidateCalendar(part, process, out _)) return process;

         process.Mode = "calendar";
         process.ReadOnly = true;

         // TODO: Paging and Sorting may be handled differently for calendar view
         var paging = ReportPaging.For(part, _settings.Settings, "calendar");
         if (paging.FetchPaged) EnforcePageSize(process, _parameters, paging);

         AddSrc(process);

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(part, process.Entities.First(), _parameters["sort"]);
         }

         // disable internal actions
         foreach (var action in process.Actions.Where(a => a.Type == "internal")) {
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


      public async Task<Process> LoadForBatchAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForBatch can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part);

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
         ConfineDataWithCachedMatcher(process, requiredFields);

         return process;
      }

      public async Task<Process> LoadForMapStreamAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForBatch can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part);

         process.Mode = "stream-map";
         process.ReadOnly = true;

         // TODO: Paging and Sorting may be handled differently for map view
         var paging = ReportPaging.For(part, _settings.Settings, "map");
         if (paging.FetchPaged) EnforcePageSize(process, _parameters, paging);

         var o = process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "geojson";
         // o.Type = "role"; // use role attribute/property (e.g. latitude, longitude, altitude, property, id)

         // some hard-coded requirements for geojson provider...
         // geojson-color used as marker-color property
         // geojson-symbol as marker-symbol property
         // geojson-size as marker-size property
         // geojson-description used as description property
         // [Ll]at* used in feature's coordinates
         // [Ll]on* used in feature's coordinates
         // batchvalue as batch-value property (and my js depends on this)
         var mapFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { string.IsNullOrWhiteSpace(part.MapDescriptionField.Text) ? "geojson-description" : part.MapDescriptionField.Text, "geojson-description" }, // becomes description property
            { string.IsNullOrWhiteSpace(part.MapLatitudeField.Text) ? "latitude" : part.MapLatitudeField.Text, "latitude" }, // used in feature coordinate
            { string.IsNullOrWhiteSpace(part.MapLongitudeField.Text) ? "longitude" : part.MapLongitudeField.Text, "longitude" } // used in feature coordinate
         };

         // { string.IsNullOrWhiteSpace(part.MapColorField.Text) ? "geojson-color" : part.MapColorField.Text, "geojson-color" },  // becomes marker-color property

         // todo: refactor the next three things
         if(part.MapColorField.Text != null && !part.MapColorField.Text.StartsWith("#")) {
            mapFields[part.MapColorField.Text] = part.MapColorField.Text;
            process.GetAllFields().First(f => f.Alias.Equals(part.MapColorField.Text, StringComparison.OrdinalIgnoreCase)).Property = true;
         }

         if (part.MapRadiusField.Text != null && !int.TryParse(part.MapRadiusField.Text, out _)) {
            mapFields[part.MapRadiusField.Text] = part.MapRadiusField.Text;
            process.GetAllFields().First(f => f.Alias.Equals(part.MapRadiusField.Text, StringComparison.OrdinalIgnoreCase)).Property = true;
         }

         if (part.MapOpacityField.Text != null && !double.TryParse(part.MapOpacityField.Text, out _)) {
            mapFields[part.MapOpacityField.Text] = part.MapOpacityField.Text;
            process.GetAllFields().First(f => f.Alias.Equals(part.MapOpacityField.Text, StringComparison.OrdinalIgnoreCase)).Property = true;
         }

         // for batch operations - currently hard-coded to batchvalue
         if (!string.IsNullOrEmpty(part.BulkActionValueField.Text)) {
            mapFields[part.BulkActionValueField.Text] = "batchvalue";
         }

         // restrict fields to only what is needed for map
         ConfineDataWithCachedMatcher(process, mapFields);

         // disable actions
         foreach (var action in process.Actions) {
            action.Before = false;
            action.After = false;
         }

         return process;
      }

      public async Task<Process> LoadForCalendarStreamAsync(ContentItem contentItem) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForCalendarStream can't load {contentItem.ContentType}.") } };
         }

         var process = await LoadInternalAsync(part);
         if (process.Status != 200 || !ValidateCalendar(part, process, out var calendarFields)) return process;

         process.Mode = "stream-calendar";
         process.ReadOnly = true;

         // TODO: Paging and Sorting may be handled differently for calendar view
         var paging = ReportPaging.For(part, _settings.Settings, "calendar");
         if (paging.FetchPaged) EnforcePageSize(process, _parameters, paging);

         AddSrc(process);

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(part, process.Entities.First(), _parameters["sort"]);
         }

         var o = process.GetOutputConnection();
         o.Stream = true;
         o.Provider = "json";

         // for batch operations - currently hard-coded to batchvalue
         if (!string.IsNullOrEmpty(part.BulkActionValueField.Text)) {
            calendarFields[part.BulkActionValueField.Text] = "batchvalue";
         }

         ConfineDataWithCachedMatcher(process, calendarFields);

         // disable actions
         foreach (var action in process.Actions) {
            action.Before = false;
            action.After = false;
         }

         return process;
      }

      private bool ValidateCalendar(TransformalizeReportPart part, Process process, out Dictionary<string, string> fields) {
         if (CalendarConfiguration.TryCreateFieldMap(part, process, out fields, out var errors)) return true;
         process.Status = 500;
         process.Message = "Calendar configuration has errors.";
         foreach (var error in errors) _logger.Error(() => error);
         return false;
      }

      public async Task<Process> LoadForTaskAsync(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null) {

         Process process;

         if (!TryGetTaskPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForTask can't load {contentItem.ContentType}.") } };
         }

         process = await LoadInternalAsync(part, parameters, format == "json" ? new JsonSerializer() : null);

         return process;
      }

      public async Task<Process> LoadForSchemaAsync(ContentItem contentItem, string format = null) {

         Process process;

         if (TryGetTaskPart(contentItem, out var taskPart)) {
            process = await LoadInternalAsync(taskPart, null, format == "json" ? new JsonSerializer() : null);
         } else if (TryGetReportPart(contentItem, out var reportPart)) {
            process = await LoadInternalAsync(reportPart, null, format == "json" ? new JsonSerializer() : null);
         } else {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForSchema can't load {contentItem.DisplayText}.") } };
         }

         return process;
      }

      public async Task<Process> LoadForParametersAsync(ContentItem contentItem, IDictionary<string, string> parameters = null) {

         Process process;

         if (!TryGetTaskPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForTask can't load {contentItem.ContentType}.") } };
         }

         process = await LoadInternalAsync(part, parameters);

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

      public async Task<Process> LoadForFormAsync(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null) {

         Process process;

         if (!TryGetFormPart(contentItem, out var part)) {
            return new Process { Status = 500, Message = "Error", Log = new List<LogEntry>() { new LogEntry(LogLevel.Error, null, $"LoadForForm can't load {contentItem.ContentType}.") } };
         }

         process = await LoadInternalAsync(part, parameters, format == "json" ? new JsonSerializer() : null);
         process.Mode = "form";
         process.ReadOnly = true;

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

      private async Task<Process> LoadInternalAsync(TransformalizeTaskPart part, IDictionary<string, string> parameters = null, ISerializer serializer = null) {
         return await LoadInternalAsync(part.Arrangement.Text, part.ContentItem, parameters, serializer);
      }

      private async Task<Process> LoadInternalAsync(TransformalizeReportPart part, IDictionary<string, string> parameters = null, ISerializer serializer = null) {
         return await LoadInternalAsync(part.Arrangement.Text, part.ContentItem, parameters, serializer);
      }

      private async Task<Process> LoadInternalAsync(TransformalizeFormPart part, IDictionary<string, string> parameters = null, ISerializer serializer = null) {
         return await LoadInternalAsync(part.Arrangement.Text, part.ContentItem, parameters, serializer);
      }

      private async Task<Process> LoadInternalAsync(string arrangement, ContentItem item, IDictionary<string, string> parameters = null, ISerializer serializer = null) {

         Process process;

         if (parameters != null) {
            foreach (var kv in parameters) {
               _parameters[kv.Key] = kv.Value;
            }
         }

         using (MiniProfiler.Current.Step("Load")) {
            _configurationContainer.Serializer = serializer;
            var scope = await _configurationContainer.CreateScopeAsync(arrangement, item, _parameters);
            process = scope.Resolve<Process>();
         }

         // in case common settings were not applied when transforming / validating parameters
         _settings.ApplyCommonSettings(process);

         // translates file content item id to full path
         if (process.Connections.Any()) {
            var connection = process.Connections.FirstOrDefault(c => _fileBasedProviders.Contains(c.Provider) && c.File.Length == Common.IdLength && !c.File.Contains('.'));
            if (connection != null) {
               var filePart = _fileService.GetFilePart(connection.File).Result;
               if (filePart != null) {
                  connection.File = System.IO.Path.Combine(_formFileStore.Path, filePart.FullPath.Text);
               }
            }
         }

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
      internal static void EnforcePageSize(Process process, IDictionary<string, string> parameters, ReportPaging paging) {
         var page = parameters.TryGetValue("page", out var rawPage) && int.TryParse(rawPage, out var parsedPage)
            && parsedPage > 0 ? parsedPage : 1;
         var size = paging.NormalizeSize(parameters.TryGetValue("size", out var rawSize) ? rawSize : null);
         foreach (var entity in process.Entities) {
            entity.Page = page;
            entity.Size = size;
         }
      }

      private void ConfineDataWithCachedMatcher(Process process, IDictionary<string, string> required) {
         ConfineData(process, required, pattern => GetFieldMatcher(process.Id, pattern));
      }

      internal Regex GetFieldMatcher(long processId, string pattern) {
         // Cache immutable matchers only, never request-owned Field instances.
         // Include the complete pattern because parameters can change the schema
         // even before the report's save signal invalidates its entries.
         var key = (typeof(ArrangementLoadService), processId, pattern);
         return _memoryCache.GetOrCreate(key, entry => {
            entry.AddExpirationToken(_signal.GetToken(Common.GetCacheKey(processId)));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
            return new Regex(pattern, RegexOptions.CultureInvariant);
         })!;
      }

      /// <summary>
      /// removes unnecessary fields to make the export faster
      /// </summary>
      /// <param name="process"></param>
      /// <param name="required"></param>
      internal static void ConfineData(Process process, IDictionary<string, string> required, Func<string, Regex>? matcherFactory = null) {

         foreach (var entity in process.Entities) {
            var all = entity.GetAllFields().ToArray();
            // Field equality/hash codes use Alias, which is changed below. Track
            // field instances and resolve the complete graph before renaming any.
            var dependencies = new HashSet<Field>(ReferenceEqualityComparer.Instance);
            var outputs = new Dictionary<Field, string>(ReferenceEqualityComparer.Instance);
            foreach (var field in all) {
               if (required.TryGetValue(field.Alias, out var alias)) {
                  dependencies.Add(field);
                  outputs.Add(field, alias);
               } else if (field.Property) {
                  dependencies.Add(field);
                  outputs.Add(field, field.Alias);
               }
            }
            // optimize download if it's not a manually written query
            if (entity.Query == string.Empty) {
               // GetFieldMatches caches Field instances globally by entity key,
               // field count and text. Those may belong to an earlier request.
               // Match names and resolve them against this request's entity.
               var pattern = @"\b(?:" + string.Join("|", all.Where(field => !field.System)
                  .OrderByDescending(field => field.Alias.Length).Select(field => Regex.Escape(field.Alias))) + @")\b";
               var fieldMatcher = matcherFactory?.Invoke(pattern) ?? new Regex(pattern, RegexOptions.CultureInvariant);
               // The upstream recursive finder starts a new visited set on each
               // call, so a cycle (including references in template text) can
               // overflow the stack. Visit each field once without recursion.
               var pending = new Stack<Field>(dependencies);
               while (pending.TryPop(out var field)) {
                  foreach (var dependency in DirectDependencies(entity, field, process.Maps, fieldMatcher)) {
                     if (dependencies.Add(dependency)) pending.Push(dependency);
                  }
               }
               foreach (var unnecessary in all.Where(field => !dependencies.Contains(field))) {
                  unnecessary.Input = false;
                  unnecessary.Output = false;
                  unnecessary.Transforms.Clear();
               }
            }
            foreach (var (field, alias) in outputs) {
               field.Output = true;
               if (!alias.Equals(field.Alias, StringComparison.OrdinalIgnoreCase)) field.Alias = alias;
            }
         }
      }

      private static IEnumerable<Field> DirectDependencies(Entity entity, Field field, IEnumerable<Map> maps, Regex fieldMatcher) {
         foreach (var parameter in field.Transforms.SelectMany(t => t.Parameters)) {
            if (!parameter.HasValue() && parameter.IsField(entity)) yield return parameter.AsField(entity);
         }

         foreach (var transform in field.Transforms) {
            if (transform.Method == "map") {
               foreach (var item in maps.First(m => m.Name == transform.Map).Items.Where(i => i.Parameter != string.Empty)) {
                  var parameter = item.AsParameter();
                  if (parameter.IsField(entity)) yield return parameter.AsField(entity);
               }
            }
            var text = string.Join(" ", transform.Value, transform.OldValue, transform.NewValue,
               transform.Format, transform.Script, transform.Expression, transform.Template);
            foreach (Match match in fieldMatcher.Matches(text)) {
               if (entity.TryGetField(match.Value, out var dependency)) yield return dependency;
            }
         }
      }

      private static bool TryGetReportPart(ContentItem contentItem, out TransformalizeReportPart part) {
         part = contentItem?.As<TransformalizeReportPart>();
         return part != null;
      }

      private static bool TryGetTaskPart(ContentItem contentItem, out TransformalizeTaskPart part) {
         part = contentItem?.As<TransformalizeTaskPart>();
         return part != null;
      }

      private static bool TryGetFormPart(ContentItem contentItem, out TransformalizeFormPart part) {
         part = contentItem?.As<TransformalizeFormPart>();
         return part != null;
      }

      private static void AddSrc(Process process) {
         if (process.Entities.Any() && process.Entities[0].Fields.Any() && string.IsNullOrEmpty(process.Entities[0].Fields.Last().Src)) {
            var fields = process.Entities[0].Fields.Where(f=>!f.System).ToArray();
            var shortened = Common.GetShortestUniqueVersions(fields.Select(f => f.Name).ToArray());
            for (int i = 0; i < shortened.Length; i++) {
               fields[i].Src = shortened[i];
            }
         }
      }
   }
}
