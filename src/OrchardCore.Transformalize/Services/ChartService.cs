using Microsoft.AspNetCore.Http;
using OrchardCore.Alias.Models;
using OrchardCore.ContentManagement;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Services;

public class ChartService : IChartService {

   private readonly IReportService _reportService;
   private readonly ISettingsService _settings;
   private readonly IHttpContextAccessor _hca;
   private readonly CombinedLogger<ChartService> _logger;

   public ChartService(
      IReportService reportService,
      ISettingsService settings,
      IHttpContextAccessor httpContextAccessor,
      CombinedLogger<ChartService> logger) {
      _hca = httpContextAccessor;
      _reportService = reportService;
      _settings = settings;
      _logger = logger;
   }

   public async Task<ChartViewModel> GetReportViewModelAsync(string contentItemId) {

      var request = new TransformalizeRequest(contentItemId) { Mode = "chart" };
      var chart = await _reportService.Validate(request);

      var part = chart.ContentItem.As<TransformalizeReportPart>();
      var aliasPart = chart.ContentItem.As<AliasPart>();
      var chartIsEnabled = part?.Chart.Value ?? false;

      if (chart.Fails()) {
         return new ChartViewModel {
            Failed = true,
            ActionResult = chart.ActionResult,
            ChartIsEnabled = chartIsEnabled,
         };
      }

      await _reportService.RunAsync(chart.Process, null);

      if (chart.Process.Status != 200) {
         return new ChartViewModel {
            ShowLog = true,
            LogViewModel = new LogViewModel(_logger.Log, chart.Process, chart.ContentItem),
            ChartIsEnabled = chartIsEnabled,
         };
      }

      var idOrAlias = aliasPart?.Alias ?? chart.ContentItem.ContentItemId;

      return new ChartViewModel {
         ReportViewModel = new ReportViewModel(chart.Process, chart.ContentItem, _hca.HttpContext!.Request.Query, idOrAlias) {
            Settings = _settings.Settings,
            IdOrAlias = idOrAlias,
            EnableInlineParameters = false,
         },
         ChartIsEnabled = chartIsEnabled,
      };
   }
}
