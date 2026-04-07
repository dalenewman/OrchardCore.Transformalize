using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {
   public interface IArrangementLoadService {
      Task<Process> LoadForReportAsync(ContentItem contentItem, string format = null);
      Task<Process> LoadForMapAsync(ContentItem contentItem);
      Task<Process> LoadForCalendarAsync(ContentItem contentItem);
      Task<Process> LoadForChartAsync(ContentItem contentItem);
      Task<Process> LoadForStreamAsync(ContentItem contentItem);
      Task<Process> LoadForTaskAsync(ContentItem contentItem, IDictionary<string,string> parameters = null, string format = null);
      Task<Process> LoadForBatchAsync(ContentItem contentItem);
      Task<Process> LoadForMapStreamAsync(ContentItem contentItem);
      Task<Process> LoadForCalendarStreamAsync(ContentItem contentItem);
      Task<Process> LoadForParametersAsync(ContentItem contentItem, IDictionary<string,string> parameters = null);
      Task<Process> LoadForFormAsync(ContentItem contentItem, IDictionary<string, string> parameters = null, string format = null);
      Task<Process> LoadForSchemaAsync(ContentItem contentItem, string format = "xml");
   }
}
