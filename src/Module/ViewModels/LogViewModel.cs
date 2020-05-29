using System.Collections.Generic;
using Module.Controllers;
using Module.Models;
using Module.Services;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using Transformalize.Logging;

namespace Module.ViewModels {
   public class LogViewModel {

      private Dictionary<string, Parameter> _parameterLookup;

      public Process Process { get; set; }
      public ContentItem Item { get; set; }
      public TransformalizeTaskPart Part { get; set;}
      public List<LogEntry> Log { get; }

      public LogViewModel(List<LogEntry> log, Process process, ContentItem item) {
         Log = log;
         Process = process;
         Item = item;
         Part = item?.As<TransformalizeTaskPart>();
      }

     public Parameter GetParameterByName(string name) {
         return ParameterLookup[name];
      }

      public Dictionary<string, Parameter> ParameterLookup {
         get {
            if (_parameterLookup != null) {
               return _parameterLookup;
            }

            _parameterLookup = new Dictionary<string, Parameter>();
            foreach (var parameter in Process.Parameters) {
               _parameterLookup[parameter.Name] = parameter;
            }

            return _parameterLookup;
         }
      }

   }
}
