using Flurl.Util;
using System.Text.RegularExpressions;
using Transformalize.Configuration;
using TransformalizeModule.Models;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Helpers;

public static class ChartHelper
{
    // The colors were defined by the designer.
    private static readonly List<string> _colorPalette =
    [
        "#FFDC00",
        "#207ED5",
        "#40A043",
        "#E66100",
        "#8A64C3",
        "#E6C700",
        "#AA9B39",
        "#276296",
        "#FF9500",
        "#B1B5B9",
        "#788087",
        "#3B4044",
    ];

    /// <summary>
    /// Builds grouped chart datasets and labels from the entity rows based on the configured chart fields.
    /// This path aggregates data (grouping/counting) for standard charts.
    /// </summary>
    public static (List<Dictionary<string, double>>, List<string>) GetDataSets(
        Entity firstEntity,
        Process process,
        string fullChartType,
        string[] chartFields)
    {
        var relevantData = new List<ChartData>();

        if (!firstEntity.GetAllOutputFields().Any(field => string.Equals(field.Alias, chartFields[0])))
        {
            var groupedData = new Dictionary<string, double>();
            var map = process.Maps.First(map => map.Name.StartsWith($"map-{chartFields[0]}"));
            map.Items.ForEach(item => groupedData.Add(item.To.ToInvariantString(), NumberFromMapItemObject(item.From.ToInvariantString())));
            return ([groupedData], groupedData.Keys.ToList());
        }

        chartFields = chartFields.Where(item => !string.IsNullOrEmpty(item)).ToArray();

        foreach (var row in firstEntity.Rows)
        {
            var chartData = new ChartData();
            foreach (var chartField in chartFields)
            {
                chartData.Data.Add(chartField, row[chartField].ToInvariantString());
            }
            relevantData.Add(chartData);
        }

        // Group by first chart field.
        var firstDataset = relevantData.GroupBy(item => item.Data[chartFields[0]]).ToList();

        // If only one chart field, just return the counts for that field.
        if (chartFields.Length == 1)
        {
            return ([firstDataset.ToDictionary(item => item.Key, item => (double)item.Count())], GetLabels(fullChartType, chartFields, relevantData));
        }

        // Count occurrences of each distinct value for distinct chartFields values.
        var datasets = new List<Dictionary<string, double>>();

        // Start at index 1 because index 0 is the grouping field.
        for (var index = 1; index < chartFields.Length; index++)
        {
            foreach (var dataValue in relevantData.DistinctBy(item => item.Data[chartFields[index]]))
            {
                var dataDictionary = new Dictionary<string, double>();

                foreach (var firstDatasetData in firstDataset)
                {
                    var count = firstDatasetData.Count(item => item.Data[chartFields[index]] == dataValue.Data[chartFields[index]]);
                    dataDictionary.Add(firstDatasetData.Key, count);
                }

                datasets.Add(dataDictionary);
            }
        }

        return (datasets, GetLabels(fullChartType, chartFields, relevantData));
    }

    /// <summary>
    /// Builds a raw series for a single chart field, preserving row order and labels exactly.
    /// Returns the datasets, axis labels, and whether any non-numeric values were found.
    /// </summary>
    public static (List<List<double>> datasets, List<string> axisLabels, bool hasNonNumeric) GetRawDataSeries(
        Entity firstEntity,
        string chartField,
        string rawDataLabelField)
    {
        var dataset = new List<double>();
        var axisLabels = new List<string>();
        var labelField = string.IsNullOrWhiteSpace(rawDataLabelField) ? chartField : rawDataLabelField;
        var hasNonNumeric = false;

        foreach (var row in firstEntity.Rows)
        {
            var label = row[labelField].ToInvariantString();
            axisLabels.Add(label);

            if (TryNumberFromField(row[chartField].ToInvariantString(), out var value))
            {
                dataset.Add(value);
            }
            else
            {
                hasNonNumeric = true;
                return ([dataset], axisLabels, hasNonNumeric);
            }
        }

        return ([dataset], axisLabels, hasNonNumeric);
    }

    public static List<string> GetColors(List<string> labels, TransformalizeReportPart part, Process process)
    {
        var backgroundColors = new List<string>();

        if (!string.IsNullOrEmpty(part.ChartColorPaletteMap.Text) &&
            process.Maps.FirstOrDefault(map => string.Equals(map.Name, part.ChartColorPaletteMap.Text.ToLower())) is { } map)
        {
            var colorMaps = new Dictionary<string, string>();
            map.Items.ForEach(item => colorMaps.Add(item.From.ToInvariantString(), item.To.ToInvariantString()));

            foreach (var label in labels)
            {
                if (colorMaps.TryGetValue(label, out var color))
                {
                    backgroundColors.Add(color);
                }
                else
                {
                    var attempts = 0;

                    if (!colorMaps.TryGetValue("Default", out var defaultColor))
                    {
                        colorMaps.TryGetValue("default", out defaultColor);
                    }

                    var useColorPalette = string.IsNullOrEmpty(defaultColor);
                    var colorPaletteCount = useColorPalette ? _colorPalette.Count : 1;
                    var nextColor = useColorPalette ? _colorPalette[attempts % colorPaletteCount] : defaultColor!;

                    while (useColorPalette &&
                        (backgroundColors.Contains(nextColor) || colorMaps.ContainsValue(nextColor)) &&
                        attempts < colorPaletteCount)
                    {
                        attempts++;
                        nextColor = _colorPalette[attempts % colorPaletteCount];
                    }
                    backgroundColors.Add(nextColor);
                }
            }
        }

        if (backgroundColors.Count < labels.Count)
        {
            var startIndex = backgroundColors.Count;
            for (var index = startIndex; index < labels.Count; index++)
            {
                backgroundColors.Add(_colorPalette[index % _colorPalette.Count]);
            }
        }

        return backgroundColors;
    }

    private static List<string> GetLabels(string fullChartType, string[] chartFields, List<ChartData> chartDataList)
    {
        var distinctValueList = new List<string[]>();
        foreach (var chartField in chartFields)
        {
            var distinctValues = chartDataList.DistinctBy(item => item.Data[chartField]).Select(item => item.Data[chartField]);
            distinctValueList.Add(distinctValues.ToArray());
        }

        if (ChartTypeHelper.IsChartMultiSeriesPie(fullChartType))
        {
            var result = new List<string>()
            {
                string.Empty,
            }.AsEnumerable();

            foreach (var group in distinctValueList)
            {
                result = result.SelectMany(
                    _ => group,
                    (prefix, value) => string.IsNullOrEmpty(prefix)
                        ? value
                        : $"{prefix} {value}"
                );
            }
            return result.ToList();
        }

        var lastDataSetDistinct = chartDataList.DistinctBy(item => item.Data[chartFields.Last()]);
        var labels = lastDataSetDistinct.Select(item => item.Data[chartFields.Last()]).ToList();
        return labels;
    }

    private static double NumberFromMapItemObject(string input)
    {
        var match = Regex.Match(input, @"\((\d+)\)");
        return double.Parse(match.Groups[1].Value);
    }

    /// <summary>
    /// Tries to convert a raw string value into a number for charting. Uses invariant culture first
    /// (e.g., "1234.56"), then current culture (e.g., "1.234,56") to handle localized formats.
    /// </summary>
    private static bool TryNumberFromField(string input, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return false;
    }
}
