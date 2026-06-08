using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Geo;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

public partial class WorldMapChart : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public List<MapDataPoint> Data { get; set; } = [];
    [Parameter] public string[] ColorScale { get; set; } = ["#dbeafe", "#2563eb"];
    [Parameter] public string NoDataColor { get; set; } = "#e5e7eb";
    [Parameter] public bool ShowLegend { get; set; } = true;
    [Parameter] public int Width { get; set; } = 900;
    [Parameter] public int Height { get; set; } = 450;
    [Parameter] public bool Responsive { get; set; }
    [Parameter] public ChartTheme? Theme { get; set; }
    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private string? _hoveredCountry;

    private const double MapViewBoxWidth = 1000;
    private const double MapViewBoxHeight = 500;

    private double ScaleX => Width / MapViewBoxWidth;
    private double ScaleY => Height / MapViewBoxHeight;

    private decimal MinValue => Data.Count > 0 ? Data.Min(d => d.Value) : 0;
    private decimal MaxValue => Data.Count > 0 ? Data.Max(d => d.Value) : 0;

    private Dictionary<string, decimal>? _dataLookup;
    private Dictionary<string, decimal> DataLookup =>
        _dataLookup ??= Data.ToDictionary(d => d.CountryCode.ToUpperInvariant(), d => d.Value);

    protected override void OnParametersSet()
    {
        _dataLookup = null;
    }

    private string GetCountryFill(string code)
    {
        if (!DataLookup.TryGetValue(code, out var value))
            return NoDataColor;
        return InterpolateColor(value);
    }

    private string InterpolateColor(decimal value)
    {
        if (ColorScale.Length == 0) return NoDataColor;
        if (ColorScale.Length == 1) return ColorScale[0];

        var range = MaxValue - MinValue;
        var t = range == 0 ? 0.5 : (double)((value - MinValue) / range);

        var segments = ColorScale.Length - 1;
        var segmentT = t * segments;
        var segmentIndex = Math.Min((int)segmentT, segments - 1);
        var localT = segmentT - segmentIndex;

        var from = ParseHex(ColorScale[segmentIndex]);
        var to = ParseHex(ColorScale[segmentIndex + 1]);

        var r = (int)(from.R + (to.R - from.R) * localT);
        var g = (int)(from.G + (to.G - from.G) * localT);
        var b = (int)(from.B + (to.B - from.B) * localT);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static (int R, int G, int B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            Convert.ToInt32(hex[..2], 16),
            Convert.ToInt32(hex[2..4], 16),
            Convert.ToInt32(hex[4..6], 16)
        );
    }

    private string GetCountryName(string code) =>
        WorldGeometry.CountryNames.TryGetValue(code, out var name) ? name : code;

    private string GetTooltipText()
    {
        if (_hoveredCountry is null) return string.Empty;
        var name = GetCountryName(_hoveredCountry);
        if (DataLookup.TryGetValue(_hoveredCountry, out var value))
            return $"{name}: {value:N0}";
        return name;
    }

    private (double X, double Y) GetTooltipPosition()
    {
        if (_hoveredCountry is null || !WorldGeometry.CountryPaths.TryGetValue(_hoveredCountry, out var path))
            return (0, 0);

        var (cx, cy) = GetPathCenter(path);
        var x = cx * ScaleX;
        var y = cy * ScaleY - 20;
        return (x, y);
    }

    private static (double X, double Y) GetPathCenter(string path)
    {
        double sumX = 0, sumY = 0;
        var count = 0;
        var parts = path.Split(' ');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (double.TryParse(parts[i], out var x) && double.TryParse(parts[i + 1], out var y))
            {
                sumX += x;
                sumY += y;
                count++;
                i++;
            }
        }
        return count > 0 ? (sumX / count, sumY / count) : (500, 250);
    }

    private void OnCountryMouseOver(string code) => _hoveredCountry = code;
    private void OnCountryMouseOut() => _hoveredCountry = null;
}
