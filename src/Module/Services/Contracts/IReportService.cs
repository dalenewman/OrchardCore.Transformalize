using Module.ViewModels;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IReportService : IReportLoadService, IReportRunService, ISortService {
      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias);
      public bool IsMissingRequiredParameters(List<Parameter> parameters);
      public IDictionary<string, string> GetParameters();
      public void SetPageSize(Process process, IDictionary<string, string> parameters, int min, int stickySize, int max);
      public ReportViewModel GetErrorModel(ContentItem contentItem, string message);
      public void ConfineData(Process process, IDictionary<string, string> required);
      public bool CanAccess(ContentItem contentItem);
   }
}
