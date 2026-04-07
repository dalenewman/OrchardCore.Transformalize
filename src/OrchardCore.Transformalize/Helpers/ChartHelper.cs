using Flurl.Util;
using System.Text.RegularExpressions;
using Transformalize.Configuration;

namespace TransformalizeModule.Helpers;

public record FacetChart(string Title, List<string> Labels, List<double> Values, string ParameterName);

public static class ChartHelper
{
    private static readonly Regex CountPattern = new(@"\((\d+)\)", RegexOptions.Compiled);

    public static List<FacetChart> GetFacetCharts(Process process)
    {
        var charts = new List<FacetChart>();
        var allFields = process.Entities.FirstOrDefault()?.GetAllFields().ToList() ?? [];

        foreach (var map in process.Maps.Where(m => m.Name.StartsWith("map-", StringComparison.OrdinalIgnoreCase)))
        {
            // map names are "map-{fieldname}-{counter}" e.g. "map-category-1"
            var withoutPrefix = map.Name["map-".Length..];
            var lastHyphen = withoutPrefix.LastIndexOf('-');
            var fieldName = lastHyphen > 0 && int.TryParse(withoutPrefix[(lastHyphen + 1)..], out _)
                ? withoutPrefix[..lastHyphen]
                : withoutPrefix;

            var field = allFields.FirstOrDefault(f => string.Equals(f.Alias, fieldName, StringComparison.OrdinalIgnoreCase));
            var title = !string.IsNullOrEmpty(field?.Label) ? field.Label
                      : !string.IsNullOrEmpty(field?.Alias) ? field.Alias
                      : fieldName;

            var labels = map.Items.Select(i => (i.To?.ToInvariantString() ?? string.Empty).Replace("''", "'")).ToList();
            var values = map.Items.Select(i => NumberFromMapItem(i.From?.ToInvariantString())).ToList();
            var parameterName = field?.Name ?? fieldName;

            charts.Add(new FacetChart(title, labels, values, parameterName));
        }

        return charts;
    }

    private static double NumberFromMapItem(string? input)
    {
        if (input == null) return 0;
        var match = CountPattern.Match(input);
        return match.Success ? double.Parse(match.Groups[1].Value) : 0;
    }
}
