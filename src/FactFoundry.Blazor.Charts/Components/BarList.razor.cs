using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A ranked "top N" breakdown list — each row shows a label and value with a horizontal
/// bar filling behind the text, its length proportional to the value. This is the compact,
/// scannable idiom used by dashboards like GA4 and Cloudflare for categorical breakdowns
/// (top pages, browsers, countries), and is a better fit than a donut or axis chart when
/// the question is simply "which are the biggest, and by how much".
/// </summary>
public partial class BarList : ComponentBase
{
    /// <summary>Optional heading rendered above the list.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Optional caption rendered under the title (e.g. "by page views").</summary>
    [Parameter] public string? Caption { get; set; }

    /// <summary>The rows to rank. Sorted by <see cref="ChartSegment.Value"/> descending before display.</summary>
    [Parameter] public List<ChartSegment> Data { get; set; } = [];

    /// <summary>Maximum number of rows to show. Defaults to 7.</summary>
    [Parameter] public int MaxItems { get; set; } = 7;

    /// <summary>When true, appends each value's share of the total as a percentage (e.g. "5 · 56%").</summary>
    [Parameter] public bool ShowShare { get; set; }

    /// <summary>.NET numeric format string for the value (e.g. "N0"). Defaults to a plain integer.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>
    /// When true, renders a small colored dot before each label using the segment's
    /// <see cref="ChartSegment.Color"/> (for status lists like Human / Bot / Security event),
    /// and keeps the bar fill a neutral accent rather than the per-row color.
    /// </summary>
    [Parameter] public bool ShowDot { get; set; }

    /// <summary>Bar fill color. Defaults to the resolved theme's first palette color.</summary>
    [Parameter] public string? AccentColor { get; set; }

    /// <summary>Opacity of the bar fill behind each row. Defaults to 0.16.</summary>
    [Parameter] public double BarOpacity { get; set; } = 0.16;

    /// <summary>Optional label text for a trailing "more" link (e.g. "View all pages").</summary>
    [Parameter] public string? MoreText { get; set; }

    /// <summary>Href for the "more" link. Required for the link to render.</summary>
    [Parameter] public string? MoreHref { get; set; }

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private string Accent => AccentColor ?? ResolvedTheme.GetColor(0);

    /// <summary>A single computed row ready for rendering.</summary>
    private readonly record struct Row(string Label, string? Dot, string ValueText, double WidthPercent);

    private IEnumerable<Row> Rows()
    {
        if (Data.Count == 0) yield break;

        var ranked = Data.OrderByDescending(d => d.Value).Take(MaxItems).ToList();
        var max = ranked.Max(d => d.Value);
        var total = Data.Sum(d => d.Value);

        foreach (var d in ranked)
        {
            var value = ValueFormat is not null
                ? d.Value.ToString(ValueFormat, CultureInfo.InvariantCulture)
                : d.Value.ToString("0.##", CultureInfo.InvariantCulture);

            if (ShowShare && total > 0)
            {
                var pct = Math.Round((double)(d.Value / total) * 100);
                value = $"{value} · {pct.ToString("0", CultureInfo.InvariantCulture)}%";
            }

            var width = max > 0 ? (double)(d.Value / max) * 100 : 0;
            yield return new Row(d.Label, ShowDot ? d.Color : null, value, width);
        }
    }

    private static string Pct(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
