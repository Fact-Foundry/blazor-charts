using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

public partial class LineChart : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public List<ChartSeries> Series { get; set; } = [];
    [Parameter] public List<string> XAxisLabels { get; set; } = [];
    [Parameter] public bool ShowLegend { get; set; } = true;
    [Parameter] public bool ShowGrid { get; set; } = true;
    [Parameter] public bool SmoothLines { get; set; }
    [Parameter] public bool ShowArea { get; set; }
    [Parameter] public bool CrosshairTooltip { get; set; }
    [Parameter] public int Width { get; set; } = 600;
    [Parameter] public int Height { get; set; } = 300;
    [Parameter] public int StrokeWidth { get; set; } = 2;
    [Parameter] public bool Responsive { get; set; }
    [Parameter] public int? MaxXAxisLabels { get; set; }
    [Parameter] public ChartTheme? Theme { get; set; }
    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private const int PaddingLeft = 50;
    private const int PaddingRight = 20;
    private const int PaddingTop = 40;
    private const int PaddingBottom = 40;
    private const int BaseLegendHeight = 30;

    private int? _hoveredSeriesIndex;
    private int? _hoveredPointIndex;
    private int? _hoveredColumnIndex;

    private int ChartAreaWidth => Width - PaddingLeft - PaddingRight;
    private int ChartAreaHeight => Height - PaddingTop - PaddingBottom - (ShowLegend ? Math.Max(BaseLegendHeight, LegendTotalHeight) : 0);

    private decimal MinValue => Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Min();
    private decimal MaxValue => Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max();

    private int PointCount => Series.Count > 0 ? Series.Max(s => s.Values.Count) : 0;

    private string GetSeriesColor(int index)
    {
        var series = Series[index];
        return series.Color ?? ResolvedTheme.GetColor(index);
    }

    private double ScaleX(int pointIndex)
    {
        if (PointCount <= 1) return PaddingLeft;
        return PaddingLeft + (double)pointIndex / (PointCount - 1) * ChartAreaWidth;
    }

    private double ScaleY(decimal value)
    {
        var range = MaxValue - MinValue;
        if (range == 0) return PaddingTop + ChartAreaHeight / 2.0;
        var normalized = (double)((value - MinValue) / range);
        return PaddingTop + ChartAreaHeight - (normalized * ChartAreaHeight);
    }

    private string BuildLinePath(ChartSeries series)
    {
        if (series.Values.Count == 0) return string.Empty;

        var points = series.Values.Select((v, i) => (X: ScaleX(i), Y: ScaleY(v))).ToList();

        if (SmoothLines && points.Count > 2)
            return BuildSmoothPath(points);

        return BuildStraightPath(points);
    }

    private static string BuildStraightPath(List<(double X, double Y)> points)
    {
        var path = $"M {points[0].X:F1} {points[0].Y:F1}";
        for (var i = 1; i < points.Count; i++)
            path += $" L {points[i].X:F1} {points[i].Y:F1}";
        return path;
    }

    private static string BuildSmoothPath(List<(double X, double Y)> points)
    {
        var path = $"M {points[0].X:F1} {points[0].Y:F1}";
        for (var i = 1; i < points.Count; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];
            var cpx = (prev.X + curr.X) / 2;
            path += $" C {cpx:F1} {prev.Y:F1}, {cpx:F1} {curr.Y:F1}, {curr.X:F1} {curr.Y:F1}";
        }
        return path;
    }

    private string BuildAreaPath(ChartSeries series)
    {
        if (series.Values.Count == 0) return string.Empty;

        var linePath = BuildLinePath(series);
        var lastX = ScaleX(series.Values.Count - 1);
        var firstX = ScaleX(0);
        var baseY = PaddingTop + ChartAreaHeight;

        return $"{linePath} L {lastX:F1} {baseY:F1} L {firstX:F1} {baseY:F1} Z";
    }

    private IEnumerable<(double Position, string Label)> GetYAxisTicks()
    {
        var range = MaxValue - MinValue;
        if (range == 0)
        {
            yield return (ScaleY(MaxValue), MaxValue.ToString("G"));
            yield break;
        }

        const int tickCount = 5;
        for (var i = 0; i <= tickCount; i++)
        {
            var value = MinValue + (range * i / tickCount);
            yield return (ScaleY(value), value.ToString("G"));
        }
    }

    private IEnumerable<(double Position, string Label)> GetXAxisTicks()
    {
        if (PointCount == 0) yield break;

        var step = GetLabelStep();
        var lastIndex = PointCount - 1;

        for (var i = 0; i < PointCount; i += step)
        {
            if (step > 1 && lastIndex - i > 0 && lastIndex - i < step)
                continue;
            var label = i < XAxisLabels.Count ? XAxisLabels[i] : i.ToString();
            yield return (ScaleX(i), label);
        }

        if (step > 1 && lastIndex % step != 0)
        {
            var label = lastIndex < XAxisLabels.Count ? XAxisLabels[lastIndex] : lastIndex.ToString();
            yield return (ScaleX(lastIndex), label);
        }
    }

    private int GetLabelStep()
    {
        if (PointCount <= 1) return 1;

        if (MaxXAxisLabels.HasValue && MaxXAxisLabels.Value > 0)
            return Math.Max(1, (int)Math.Ceiling((double)PointCount / MaxXAxisLabels.Value));

        var avgLabelWidth = XAxisLabels.Count > 0
            ? XAxisLabels.Average(l => l.Length) * 7 + 12
            : 30;
        var slotWidth = (double)ChartAreaWidth / (PointCount - 1);
        if (slotWidth >= avgLabelWidth) return 1;

        return (int)Math.Ceiling(avgLabelWidth / slotWidth);
    }

    private void OnPointMouseOver(int seriesIndex, int pointIndex)
    {
        _hoveredSeriesIndex = seriesIndex;
        _hoveredPointIndex = pointIndex;
    }

    private void OnPointMouseOut()
    {
        _hoveredSeriesIndex = null;
        _hoveredPointIndex = null;
    }

    private void OnColumnMouseOver(int pointIndex) => _hoveredColumnIndex = pointIndex;
    private void OnColumnMouseOut() => _hoveredColumnIndex = null;

    private string GetTooltipText()
    {
        if (_hoveredSeriesIndex is null || _hoveredPointIndex is null) return string.Empty;
        var series = Series[_hoveredSeriesIndex.Value];
        var value = series.Values[_hoveredPointIndex.Value];
        var label = _hoveredPointIndex.Value < XAxisLabels.Count
            ? XAxisLabels[_hoveredPointIndex.Value]
            : _hoveredPointIndex.Value.ToString();
        return $"{series.Label}: {value} ({label})";
    }

    private bool IsPointHovered(int seriesIndex, int pointIndex)
    {
        if (CrosshairTooltip)
            return _hoveredColumnIndex == pointIndex;
        return _hoveredSeriesIndex == seriesIndex && _hoveredPointIndex == pointIndex;
    }

    private List<(double X, double Y, int SeriesIndex)>? _legendLayoutCache;
    private int _legendLayoutHash;

    private List<(double X, double Y, int SeriesIndex)> GetLegendLayout()
    {
        var hash = HashCode.Combine(Series.Count, Width, ShowLegend,
            Series.Count > 0 ? Series.Sum(s => s.Label.Length) : 0);
        if (_legendLayoutCache is not null && _legendLayoutHash == hash)
            return _legendLayoutCache;

        var items = new List<(double X, double Y, int SeriesIndex)>();
        var availableWidth = Width - PaddingLeft - PaddingRight;
        var rowHeight = 20.0;
        var itemGap = 16.0;
        var swatchWidth = 16.0;

        double curX = PaddingLeft;
        int row = 0;

        for (var i = 0; i < Series.Count; i++)
        {
            var labelWidth = Series[i].Label.Length * 7 + swatchWidth + itemGap;
            if (i > 0 && curX + labelWidth - PaddingLeft > availableWidth)
            {
                row++;
                curX = PaddingLeft;
            }
            items.Add((curX, row * rowHeight, i));
            curX += labelWidth;
        }

        _legendLayoutCache = items;
        _legendLayoutHash = hash;
        return items;
    }

    private int LegendTotalHeight
    {
        get
        {
            if (!ShowLegend || Series.Count == 0) return 0;
            var layout = GetLegendLayout();
            if (layout.Count == 0) return 0;
            var rows = (int)(layout.Max(l => l.Y) / 20.0) + 1;
            return rows * 20 + 10;
        }
    }

    private (double X, double Y, double BoxWidth, double BoxHeight) GetCrosshairTooltipLayout()
    {
        if (_hoveredColumnIndex is null) return (0, 0, 0, 0);

        var cx = ScaleX(_hoveredColumnIndex.Value);
        var rowHeight = 18.0;
        var boxWidth = 170.0;
        var boxHeight = 26 + Series.Count * rowHeight + 4;
        var boxX = cx + 12;
        if (boxX + boxWidth > Width - PaddingRight)
            boxX = cx - boxWidth - 12;
        var boxY = (double)PaddingTop + 4;

        return (boxX, boxY, boxWidth, boxHeight);
    }
}
