using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

public partial class BarChart : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public List<ChartSeries> Series { get; set; } = [];
    [Parameter] public List<string> XAxisLabels { get; set; } = [];
    [Parameter] public bool ShowLegend { get; set; } = true;
    [Parameter] public bool ShowGrid { get; set; } = true;
    [Parameter] public bool Horizontal { get; set; }
    [Parameter] public bool Stacked { get; set; }
    [Parameter] public bool CrosshairTooltip { get; set; }
    [Parameter] public int Width { get; set; } = 600;
    [Parameter] public int Height { get; set; } = 300;
    [Parameter] public ChartTheme? Theme { get; set; }
    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private int PaddingLeft => Horizontal ? 80 : 50;
    private const int PaddingRight = 20;
    private const int PaddingTop = 40;
    private const int PaddingBottom = 40;
    private const int LegendHeight = 30;

    private int? _hoveredSeriesIndex;
    private int? _hoveredCategoryIndex;

    private int ChartAreaWidth => Width - PaddingLeft - PaddingRight;
    private int ChartAreaHeight => Height - PaddingTop - PaddingBottom - (ShowLegend ? LegendHeight : 0);

    private int CategoryCount => Series.Count > 0 ? Series.Max(s => s.Values.Count) : 0;

    private decimal MaxValue
    {
        get
        {
            if (Series.Count == 0) return 0;
            if (Stacked)
            {
                decimal max = 0;
                for (var i = 0; i < CategoryCount; i++)
                {
                    var sum = Series.Sum(s => i < s.Values.Count ? s.Values[i] : 0);
                    if (sum > max) max = sum;
                }
                return max;
            }
            return Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max();
        }
    }

    private string GetSeriesColor(int index)
    {
        var series = Series[index];
        return series.Color ?? ResolvedTheme.GetColor(index);
    }

    private double ScaleValue(decimal value)
    {
        var max = MaxValue;
        if (max == 0) return 0;
        return (double)(value / max);
    }

    private double GetCategoryCenter(int categoryIndex)
    {
        if (CategoryCount == 0) return 0;
        var groupWidth = (double)(Horizontal ? ChartAreaHeight : ChartAreaWidth) / CategoryCount;
        return groupWidth * categoryIndex + groupWidth / 2;
    }

    private (double X, double Y, double W, double H) GetBarRect(int seriesIndex, int categoryIndex)
    {
        if (CategoryCount == 0) return (0, 0, 0, 0);

        var value = categoryIndex < Series[seriesIndex].Values.Count
            ? Series[seriesIndex].Values[categoryIndex]
            : 0;

        if (Horizontal)
            return GetHorizontalBarRect(seriesIndex, categoryIndex, value);
        return GetVerticalBarRect(seriesIndex, categoryIndex, value);
    }

    private (double X, double Y, double W, double H) GetVerticalBarRect(int seriesIndex, int categoryIndex, decimal value)
    {
        var groupWidth = (double)ChartAreaWidth / CategoryCount;
        var barPadding = groupWidth * 0.15;
        var usableWidth = groupWidth - barPadding * 2;

        double barWidth, barX;
        if (Stacked)
        {
            barWidth = usableWidth;
            barX = PaddingLeft + groupWidth * categoryIndex + barPadding;
        }
        else
        {
            barWidth = usableWidth / Series.Count;
            barX = PaddingLeft + groupWidth * categoryIndex + barPadding + barWidth * seriesIndex;
        }

        var barHeight = ScaleValue(value) * ChartAreaHeight;
        var barY = PaddingTop + ChartAreaHeight - barHeight;

        if (Stacked)
        {
            var stackBelow = GetStackOffset(seriesIndex, categoryIndex);
            barY -= ScaleValue(stackBelow) * ChartAreaHeight;
        }

        return (barX, barY, barWidth, barHeight);
    }

    private (double X, double Y, double W, double H) GetHorizontalBarRect(int seriesIndex, int categoryIndex, decimal value)
    {
        var groupHeight = (double)ChartAreaHeight / CategoryCount;
        var barPadding = groupHeight * 0.15;
        var usableHeight = groupHeight - barPadding * 2;

        double barHeight, barY;
        if (Stacked)
        {
            barHeight = usableHeight;
            barY = PaddingTop + groupHeight * categoryIndex + barPadding;
        }
        else
        {
            barHeight = usableHeight / Series.Count;
            barY = PaddingTop + groupHeight * categoryIndex + barPadding + barHeight * seriesIndex;
        }

        var barWidth = ScaleValue(value) * ChartAreaWidth;
        var barX = (double)PaddingLeft;

        if (Stacked)
        {
            var stackBefore = GetStackOffset(seriesIndex, categoryIndex);
            barX += ScaleValue(stackBefore) * ChartAreaWidth;
        }

        return (barX, barY, barWidth, barHeight);
    }

    private decimal GetStackOffset(int seriesIndex, int categoryIndex)
    {
        decimal sum = 0;
        for (var i = 0; i < seriesIndex; i++)
            sum += categoryIndex < Series[i].Values.Count ? Series[i].Values[categoryIndex] : 0;
        return sum;
    }

    private IEnumerable<(double Position, string Label)> GetValueAxisTicks()
    {
        var max = MaxValue;
        if (max == 0)
        {
            yield return (Horizontal ? (double)PaddingLeft : PaddingTop + ChartAreaHeight, "0");
            yield break;
        }

        const int tickCount = 5;
        for (var i = 0; i <= tickCount; i++)
        {
            var value = max * i / tickCount;
            var ratio = (double)(value / max);

            double position;
            if (Horizontal)
                position = PaddingLeft + ratio * ChartAreaWidth;
            else
                position = PaddingTop + ChartAreaHeight - ratio * ChartAreaHeight;

            yield return (position, value.ToString("G"));
        }
    }

    private IEnumerable<(double Position, string Label)> GetCategoryAxisTicks()
    {
        for (var i = 0; i < CategoryCount; i++)
        {
            var label = i < XAxisLabels.Count ? XAxisLabels[i] : i.ToString();
            var center = GetCategoryCenter(i);

            double position;
            if (Horizontal)
                position = PaddingTop + center;
            else
                position = PaddingLeft + center;

            yield return (position, label);
        }
    }

    private void OnBarMouseOver(int seriesIndex, int categoryIndex)
    {
        _hoveredSeriesIndex = seriesIndex;
        _hoveredCategoryIndex = categoryIndex;
    }

    private void OnBarMouseOut()
    {
        _hoveredSeriesIndex = null;
        _hoveredCategoryIndex = null;
    }

    private void OnCategoryMouseOver(int categoryIndex) => _hoveredCategoryIndex = categoryIndex;
    private void OnCategoryMouseOut() => _hoveredCategoryIndex = null;

    private bool IsBarHovered(int seriesIndex, int categoryIndex)
    {
        if (CrosshairTooltip)
            return _hoveredCategoryIndex == categoryIndex;
        return _hoveredSeriesIndex == seriesIndex && _hoveredCategoryIndex == categoryIndex;
    }

    private string GetSingleTooltipText()
    {
        if (_hoveredSeriesIndex is null || _hoveredCategoryIndex is null) return string.Empty;
        var series = Series[_hoveredSeriesIndex.Value];
        var value = _hoveredCategoryIndex.Value < series.Values.Count
            ? series.Values[_hoveredCategoryIndex.Value] : 0;
        var label = _hoveredCategoryIndex.Value < XAxisLabels.Count
            ? XAxisLabels[_hoveredCategoryIndex.Value]
            : _hoveredCategoryIndex.Value.ToString();
        return $"{series.Label}: {value:N0} ({label})";
    }

    private (double X, double Y, double BoxWidth, double BoxHeight) GetCrosshairTooltipPos()
    {
        if (_hoveredCategoryIndex is null) return (0, 0, 0, 0);

        var rowHeight = 18.0;
        var boxWidth = 170.0;
        var boxHeight = 26 + Series.Count * rowHeight + 4;

        double boxX, boxY;
        if (Horizontal)
        {
            var center = PaddingTop + GetCategoryCenter(_hoveredCategoryIndex.Value);
            boxX = PaddingLeft + ChartAreaWidth * 0.5 - boxWidth / 2;
            boxY = center + 12;
            if (boxY + boxHeight > PaddingTop + ChartAreaHeight)
                boxY = center - boxHeight - 12;
        }
        else
        {
            var center = PaddingLeft + GetCategoryCenter(_hoveredCategoryIndex.Value);
            boxX = center + 12;
            if (boxX + boxWidth > Width - PaddingRight)
                boxX = center - boxWidth - 12;
            boxY = (double)PaddingTop + 4;
        }

        return (boxX, boxY, boxWidth, boxHeight);
    }
}
