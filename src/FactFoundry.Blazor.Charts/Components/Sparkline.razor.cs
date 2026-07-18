using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A minimal inline trend line — a bare area + stroke with no axes, labels, grid, or
/// legend — sized to sit inside a KPI tile beside a headline number. Unlike
/// <c>LineChart</c>, it reserves no room for axis chrome, so it stays legible at tiny
/// sizes (e.g. 80×30).
/// </summary>
public partial class Sparkline : ComponentBase
{
    /// <summary>The values to plot, oldest to newest.</summary>
    [Parameter] public List<decimal> Values { get; set; } = [];

    /// <summary>Line and fill color. Defaults to the resolved theme's first palette color.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>ViewBox width. With <see cref="Responsive"/>, defines the aspect ratio.</summary>
    [Parameter] public int Width { get; set; } = 82;

    /// <summary>ViewBox height. With <see cref="Responsive"/>, defines the aspect ratio.</summary>
    [Parameter] public int Height { get; set; } = 30;

    /// <summary>Stroke width of the line.</summary>
    [Parameter] public double StrokeWidth { get; set; } = 1.6;

    /// <summary>Draw the gradient area beneath the line. Defaults to true.</summary>
    [Parameter] public bool ShowArea { get; set; } = true;

    /// <summary>Draw a dot on the most recent point. Defaults to true.</summary>
    [Parameter] public bool ShowEndDot { get; set; } = true;

    /// <summary>Scale to the container width (SVG <c>width="100%"</c>). Defaults to true.</summary>
    [Parameter] public bool Responsive { get; set; } = true;

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private string Stroke => Color ?? ResolvedTheme.GetColor(0);

    // A gradient id unique to this instance so multiple sparklines don't share a fill.
    private readonly string _gradientId = "ffspark-" + Guid.NewGuid().ToString("N")[..8];

    private const double Pad = 2;

    private static string F(double d) => d.ToString("F1", CultureInfo.InvariantCulture);

    private List<(double X, double Y)> Points()
    {
        if (Values.Count == 0) return [];
        var max = (double)Values.Max();
        if (max <= 0) max = 1;
        var n = Values.Count;

        double X(int i) => n <= 1 ? Width / 2.0 : Pad + i * (Width - 2 * Pad) / (n - 1.0);
        double Y(decimal v) => Height - Pad - (double)v / max * (Height - 2 * Pad);

        return Values.Select((v, i) => (X(i), Y(v))).ToList();
    }

    private string LinePath(List<(double X, double Y)> pts) =>
        pts.Count == 0 ? "" : string.Join(" ", pts.Select((p, i) => $"{(i == 0 ? "M" : "L")}{F(p.X)} {F(p.Y)}"));

    private string AreaPath(List<(double X, double Y)> pts) =>
        pts.Count == 0 ? "" : $"{LinePath(pts)} L{F(pts[^1].X)} {F(Height)} L{F(pts[0].X)} {F(Height)} Z";
}
