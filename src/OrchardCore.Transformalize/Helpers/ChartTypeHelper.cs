namespace TransformalizeModule.Helpers;

public static class ChartTypeHelper
{
    public static string GetChartType(string chartType)
    {
        var split = chartType.Split(" ");
        var type = split.Length > 1 ? split[1] : split[0];
        return type;
    }

    public static bool IsChartStacked(string chartType) => chartType.StartsWith("stacked");
    public static bool IsChartPointsStyled(string chartType) => chartType.StartsWith("points");
    public static bool IsChartCombined(string chartType) => chartType.Split(" ").Length > 2;
    public static bool IsChartHorizontal(string chartType) => chartType.StartsWith("horizontal");
    public static bool IsChartMultiSeriesPie(string chartType) => chartType.StartsWith("multi");
}
