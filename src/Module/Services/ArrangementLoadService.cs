using Autofac;
using Module.Services.Modifiers;
using Module.Services.Contracts;
using System.Collections.Generic;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using StackExchange.Profiling;
using OrchardCore.ContentManagement;
using System.Linq;
using System;
using Cfg.Net.Contracts;
using Transformalize.Validate.Jint.Autofac;
using Module.Models;
using Cfg.Net.Serializers;
using Transformalize.Logging;
using Module.Transforms;
using Microsoft.AspNetCore.Http;
using Transformalize.Context;

namespace Module.Services {
   public class ArrangementLoadService : IArrangementLoadService {

      private readonly IStickyParameterService _stickyParameterService;
      private readonly IDictionary<string, string> _parameters;
      private readonly ISortService _sortService;
      private readonly ISettingsService _settings;
      private readonly IHttpContextAccessor _httpContext;

      public ArrangementLoadService(
         IParameterService parameterService,
         IStickyParameterService stickyParameterService,
         ISortService sortService,
         ISettingsService settings,
         IHttpContextAccessor httpContext
      ) {
         _stickyParameterService = stickyParameterService;
         _parameters = parameterService.GetParameters();
         _sortService = sortService;
         _settings = settings;
         _httpContext = httpContext;
      }

      public Process LoadForExport(ContentItem contentItem, IPipelineLogger logger) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process() { Status = 500, Message = $"Content item is not compatible with Transformalize." };
         }

         var process = LoadInternal(part.Arrangement.Arrangement, logger);

         process.Mode = "report";
         process.ReadOnly = true;

         if (part.PageSizes.Enabled()) {
            var size = process.Connections.First().Provider == "bogus" ? _settings.GetPageSizes(part).Max() : 0;
            EnforcePageSize(process, _parameters, size, size, size);
         }

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(process.Entities.First(), _parameters["sort"]);
         }

         foreach (var entity in process.Entities) {

            foreach (var field in entity.GetAllFields()) {
               if (field.System) {
                  field.Output = false;
               }
               field.Output = field.Output && field.Export == "defer" || field.Export == "true";
            }
         }

         return process;
      }

      public Process LoadForReport(ContentItem contentItem, IPipelineLogger logger, string format = null) {

         if (!TryGetReportPart(contentItem, out var part)) {
            return new Process() { Status = 500, Message = $"Content item is not compatible with Transformalize." };
         }

         _stickyParameterService.GetStickyParameters(contentItem.ContentItemId, _parameters);

         var process = LoadInternal(part.Arrangement.Arrangement, logger, format == "json" ? new JsonSerializer() : null);

         process.Mode = "report";
         process.ReadOnly = true;

         _stickyParameterService.SetStickyParameters(contentItem.ContentItemId, process.Parameters);
         

         if (part.PageSizes.Enabled()) {
            var pageSizes = _settings.GetPageSizes(part);
            var stickySize = _stickyParameterService.GetStickyParameter(contentItem.ContentItemId, "size", () => pageSizes.Min());
            EnforcePageSize(process, _parameters, pageSizes.Min(), stickySize, pageSizes.Max());
         }         

         if (_parameters.ContainsKey("sort") && _parameters["sort"] != null) {
            _sortService.AddSortToEntity(process.Entities.First(), _parameters["sort"]);
         }

         foreach (var connection in process.Connections) {
            connection.Buffer = true;
         }
         return process;
      }

      public Process LoadForTask(ContentItem contentItem, IPipelineLogger logger, string format = null) {

         if (!TryGetTaskPart(contentItem, out var part)) {
            return new Process() { Status = 500, Message = "Error" };
         }

         var process = LoadInternal(part.Arrangement.Arrangement, logger, format == "json" ? new JsonSerializer() : null);

         return process;
      }

      private Process LoadInternal(string arrangement, IPipelineLogger logger, ISerializer serializer = null) {

         Process process;

         using (MiniProfiler.Current.Step("Load")) {

            var container = new ConfigurationContainer();

            // configuration customizers
            container.AddDependency(new ReportParameterModifier());

            if (serializer != null) {
               container.AddDependency(serializer);
            }

            // transforms
            container.AddTransform((c) => new UsernameTransform(_httpContext, c), new UsernameTransform().GetSignatures());

            // external transforms register their short-hand here
            container.AddModule(new JintTransformModule());
            container.AddModule(new JsonTransformModule());
            container.AddModule(new HumanizeModule());

            // extrenal validators register their short-hand here
            container.AddModule(new JintValidateModule());

            process = container.CreateScope(arrangement, logger, _parameters).Resolve<Process>();

         }

         ApplyCommonSettings(process);

         if (process.Errors().Any() || logger is MemoryLogger m && m.Log.Any(l => l.LogLevel == LogLevel.Error)) {
            if(logger is MemoryLogger ml) {
               process.Log.AddRange(ml.Log);
            }
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
      /// when we get to the point we're streaming lots geo-json data from complex reports, 
      /// this removes unnecessary stuff from the main report definition
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
         part = contentItem.As<TransformalizeReportPart>();
         return part != null;
      }

      private bool TryGetTaskPart(ContentItem contentItem, out TransformalizeTaskPart part) {
         part = contentItem.As<TransformalizeTaskPart>();
         return part != null;
      }

   }
}
