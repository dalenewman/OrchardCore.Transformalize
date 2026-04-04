using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Services.Contracts;

public interface IChartService
{
    Task<ChartViewModel> GetReportViewModelAsync(string contentItemId);
}
