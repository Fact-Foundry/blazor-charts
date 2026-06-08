namespace FactFoundry.Blazor.Charts.Models;

public class ChartSeries
{
    public string Label { get; set; } = string.Empty;
    public string? Color { get; set; }
    public List<decimal> Values { get; set; } = [];
}
