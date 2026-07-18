using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

public partial class PieChart : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public List<ChartSegment> Data { get; set; } = [];
    [Parameter] public int MaxSegments { get; set; } = 0;
    [Parameter] public bool ShowLegend { get; set; } = true;
    [Parameter] public bool ShowLegendValues { get; set; }
    [Parameter] public bool ShowLabels { get; set; }
    [Parameter] public bool ShowLabelName { get; set; } = true;
    [Parameter] public bool ShowLabelValue { get; set; } = true;
    [Parameter] public bool ShowLabelPercent { get; set; } = true;
    [Parameter] public int Width { get; set; } = 300;
    [Parameter] public int Height { get; set; } = 300;
    [Parameter] public bool Responsive { get; set; }
    [Parameter] public ChartTheme? Theme { get; set; }
    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    /// <summary>Accessible description read by screen readers, emitted as the SVG <c>&lt;desc&gt;</c>. Auto-generated from the data when unset.</summary>
    [Parameter] public string? Description { get; set; }

    private readonly string _a11yId = "ffc-" + Guid.NewGuid().ToString("N")[..8];
    private string TitleId => _a11yId + "t";
    private string DescId => _a11yId + "d";
    private string AccessibleName => string.IsNullOrEmpty(Title) ? "Pie chart" : Title!;
    private string AccessibleDescription => Description ?? BuildAccessibleDescription();

    private string BuildAccessibleDescription()
    {
        if (Data.Count == 0) return "Pie chart with no data.";
        var total = Data.Sum(d => d.Value);
        var top = Data.OrderByDescending(d => d.Value).First();
        var pct = total > 0 ? (int)Math.Round((double)(top.Value / total) * 100) : 0;
        return $"Pie chart of {Data.Count} segments. Largest is {top.Label} at {pct}%.";
    }

    private int LegendWidth => ShowLegendValues ? 210 : 140;
    private const int LabelMargin = 60;
    private const double MinLabelSpacing = 14;

    private int? _hoveredIndex;

    private double CenterX => (Width - (ShowLegend ? LegendWidth : 0)) / 2.0;
    private double CenterY => Height / 2.0 + (string.IsNullOrEmpty(Title) ? 0 : 10);
    private double Radius => Math.Min(
        Width - (ShowLegend ? LegendWidth : 0) - (ShowLabels ? LabelMargin * 2 : 0),
        Height - 40 - (ShowLabels ? LabelMargin * 2 : 0)) / 2.0 - 10;

    private List<ChartSegment> EffectiveData => GetEffectiveData();

    private List<ChartSegment> GetEffectiveData()
    {
        if (Data.Count == 0) return [];
        if (MaxSegments <= 0 || Data.Count <= MaxSegments) return Data;

        var sorted = Data.OrderByDescending(s => s.Value).ToList();
        var top = sorted.Take(MaxSegments - 1).ToList();
        var otherValue = sorted.Skip(MaxSegments - 1).Sum(s => s.Value);
        top.Add(new ChartSegment { Label = "Other", Value = otherValue, Color = "#9CA3AF" });
        return top;
    }

    private decimal Total => EffectiveData.Sum(s => s.Value);

    private string GetSegmentColor(int index)
    {
        var segment = EffectiveData[index];
        return segment.Color ?? ResolvedTheme.GetColor(index);
    }

    private string BuildSegmentPath(int index)
    {
        var total = Total;
        if (total == 0) return string.Empty;

        var startAngle = GetStartAngle(index);
        var sweepAngle = (double)(EffectiveData[index].Value / total) * 360;

        if (sweepAngle >= 359.99)
        {
            return BuildFullCirclePath();
        }

        var radius = _hoveredIndex == index ? Radius + 4 : Radius;

        var startRad = startAngle * Math.PI / 180;
        var endRad = (startAngle + sweepAngle) * Math.PI / 180;

        var startX = CenterX + radius * Math.Cos(startRad);
        var startY = CenterY + radius * Math.Sin(startRad);
        var endX = CenterX + radius * Math.Cos(endRad);
        var endY = CenterY + radius * Math.Sin(endRad);

        var largeArc = sweepAngle > 180 ? 1 : 0;

        return $"M {CenterX:F2} {CenterY:F2} " +
               $"L {startX:F2} {startY:F2} " +
               $"A {radius:F2} {radius:F2} 0 {largeArc} 1 {endX:F2} {endY:F2} Z";
    }

    private string BuildFullCirclePath()
    {
        var radius = _hoveredIndex == 0 ? Radius + 4 : Radius;
        return $"M {CenterX + radius:F2} {CenterY:F2} " +
               $"A {radius:F2} {radius:F2} 0 1 1 {CenterX - radius:F2} {CenterY:F2} " +
               $"A {radius:F2} {radius:F2} 0 1 1 {CenterX + radius:F2} {CenterY:F2}";
    }

    private double GetStartAngle(int index)
    {
        var total = Total;
        if (total == 0) return -90;
        double angle = -90;
        for (var i = 0; i < index; i++)
            angle += (double)(EffectiveData[i].Value / total) * 360;
        return angle;
    }

    private double GetMidAngle(int index)
    {
        var total = Total;
        if (total == 0) return -90;
        var startAngle = GetStartAngle(index);
        var sweepAngle = (double)(EffectiveData[index].Value / total) * 360;
        return startAngle + sweepAngle / 2;
    }

    private string GetPercentage(int index)
    {
        var total = Total;
        if (total == 0) return "0%";
        return $"{EffectiveData[index].Value / total * 100:F1}%";
    }

    private record struct LabelPosition(
        int Index, double EdgeX, double EdgeY,
        double ElbowX, double ElbowY,
        double EndX, double EndY, string Anchor, string Color, string Text);

    private List<LabelPosition> GetResolvedLabels()
    {
        var labels = new List<LabelPosition>();
        for (var i = 0; i < EffectiveData.Count; i++)
        {
            var midAngleRad = GetMidAngle(i) * Math.PI / 180;
            var edgeX = CenterX + Radius * Math.Cos(midAngleRad);
            var edgeY = CenterY + Radius * Math.Sin(midAngleRad);

            var elbowRadius = Radius + 14;
            var elbowX = CenterX + elbowRadius * Math.Cos(midAngleRad);
            var elbowY = CenterY + elbowRadius * Math.Sin(midAngleRad);

            var isRight = Math.Cos(midAngleRad) >= 0;
            var endX = elbowX + (isRight ? 30 : -30);
            var rightBound = ShowLegend ? Width - LegendWidth - 8.0 : Width - 8.0;
            endX = Math.Clamp(endX, 8.0, rightBound);

            var anchor = isRight ? "start" : "end";
            var color = GetSegmentColor(i);
            var text = GetLabelText(i);

            labels.Add(new LabelPosition(i, edgeX, edgeY, elbowX, elbowY, endX, elbowY, anchor, color, text));
        }

        ResolveCollisions(labels, "start");
        ResolveCollisions(labels, "end");

        return labels;
    }

    private static void ResolveCollisions(List<LabelPosition> labels, string anchor)
    {
        var side = labels.Where(l => l.Anchor == anchor).OrderBy(l => l.EndY).ToList();
        if (side.Count < 2) return;

        for (var i = 1; i < side.Count; i++)
        {
            var gap = side[i].EndY - side[i - 1].EndY;
            if (gap < MinLabelSpacing)
            {
                var push = MinLabelSpacing - gap;
                for (var j = i; j < side.Count; j++)
                {
                    var s = side[j];
                    side[j] = s with { EndY = s.EndY + push };
                }
            }
        }

        for (var i = 0; i < side.Count; i++)
        {
            var idx = labels.FindIndex(l => l.Index == side[i].Index);
            labels[idx] = side[i];
        }
    }

    private string GetLabelText(int index)
    {
        var segment = EffectiveData[index];
        var parts = new List<string>();

        if (ShowLabelName) parts.Add(segment.Label);
        if (ShowLabelValue) parts.Add(segment.Value.ToString("N0"));

        var text = string.Join(": ", parts);

        if (ShowLabelPercent)
        {
            var pct = GetPercentage(index);
            text = text.Length > 0 ? $"{text} ({pct})" : pct;
        }

        return text.Length > 0 ? text : segment.Label;
    }

    private string GetLegendText(int index)
    {
        var segment = EffectiveData[index];
        if (!ShowLegendValues) return segment.Label;
        return $"{segment.Label}: {segment.Value:N0} ({GetPercentage(index)})";
    }

    private void OnSegmentMouseOver(int index) => _hoveredIndex = index;
    private void OnSegmentMouseOut() => _hoveredIndex = null;
}
