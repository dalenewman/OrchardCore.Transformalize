using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement;
using OrchardCore.Title.Models;
using Transformalize.Configuration;
using TransformalizeModule.Models;

namespace TransformalizeModule.ViewModels {
   public class ReportViewModel {

      private Dictionary<string, Parameter> _parameterLookup;
      private Dictionary<string, Parameter> _inlines;
      private Process _process;
      private HashSet<string> _topParameters;
      private readonly IQueryCollection _queryCollection;

      public TransformalizeSettings Settings { get; set; }

      // temps
      public bool EnableInlineParameters { get; set; } = true;

      public bool Editable { get; set; } = false;
      public bool CalendarEnabled { get; set; }
      public string IdOrAlias { get; set; }
      public string Title { get; set; }

      public Process Process {
         get {
            return _process;
         }

         set {
            _process = value;
            _topParameters = null;
            _inlines = null;
         }
      }

      public ContentItem Item { get; set; }
      public TransformalizeReportPart Part { get; set; }

      public List<BreadCrumb> BreadCrumbs { get; set; } = new List<BreadCrumb>();

      public ReportViewModel(
         Process process,
         ContentItem item,
         IQueryCollection queryCollection,
         string idOrAlias
      ) {
         Process = process;
         Item = item;
         Part = item.As<TransformalizeReportPart>();
         IdOrAlias = idOrAlias;
         Title = item.As<TitlePart>().Title;
         _queryCollection = queryCollection;
      }

      public Dictionary<string, Parameter> InlineParameters {
         get {
            if (_inlines != null) {
               return _inlines;
            }
            CalculateWhereParametersGo();
            return _inlines;
         }
      }

      private void CalculateWhereParametersGo() {

         _inlines = new Dictionary<string, Parameter>();
         _topParameters = new HashSet<string>();
         foreach (var parameter in Process.Parameters.Where(p => p.Prompt)) {
            TopParameters.Add(parameter.Name);
         }

         foreach (var field in Process.Entities.First().GetAllFields().Where(f => !f.System && f.Output)) {

            // opt out of inline field consideration
            if (field.Parameter != null && field.Parameter.Equals("None", StringComparison.OrdinalIgnoreCase)) {
               continue;
            }

            if (field.Parameter != null && ParameterLookup.TryGetValue(field.Parameter, out Parameter? v1) && v1.Prompt && !v1.Required) {
               _inlines[field.Alias] = v1;
               _topParameters.Remove(field.Parameter);
            } else if (ParameterLookup.TryGetValue(field.Alias, out Parameter? v2) && v2.Prompt && !v2.Required) {
               _inlines[field.Alias] = v2;
               _topParameters.Remove(field.Alias);
            } else if (ParameterLookup.TryGetValue(field.SortField, out Parameter? v3) && v3.Prompt && !v3.Required) {
               _inlines[field.Alias] = v3;
               _topParameters.Remove(field.SortField);
            }
         }
      }

      public HashSet<string> TopParameters {
         get {
            if (_topParameters != null) {
               return _topParameters;
            }
            CalculateWhereParametersGo();
            return _topParameters;
         }
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

      public string QueryValue(string key) {
            return _queryCollection[key].ToString() ?? string.Empty;
      }

   }
}
