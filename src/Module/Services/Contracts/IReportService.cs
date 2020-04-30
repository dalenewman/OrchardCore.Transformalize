using Module.ViewModels;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IReportService : IReportLoadService, IReportRunService {
      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias);
      public bool IsMissingRequiredParameters(List<Parameter> parameters);
      public ReportViewModel GetErrorModel(ContentItem contentItem, string message);
      public bool CanAccess(ContentItem contentItem);
   }
}
