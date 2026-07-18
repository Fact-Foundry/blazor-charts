using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A GitHub-contributions-style calendar heatmap — a grid of day cells laid out in
/// week columns and weekday rows, each colored by its value's intensity. Rendered as
/// pure Razor-to-SVG in the same idiom as the other charts and themed through
/// <c>ChartThemeProvider</c>. Hand it a flat list of <see cref="CalendarPoint"/> (day +
/// value); points sharing a date are summed, and the grid spans <see cref="StartDate"/>
/// to <see cref="EndDate"/> when given, otherwise the data's own date range.
/// </summary>
public partial class CalendarHeatmap : ComponentBase
{
    /// <summary>The per-day values to plot. Points on the same date are summed.</summary>
    [Parameter] public List<CalendarPoint> Data { get; set; } = [];

    /// <summary>First day of the grid. Defaults to the earliest date in <see cref="Data"/>.</summary>
    [Parameter] public DateOnly? StartDate { get; set; }

    /// <summary>Last day of the grid. Defaults to the latest date in <see cref="Data"/>.</summary>
    [Parameter] public DateOnly? EndDate { get; set; }

    /// <summary>Which weekday the columns start on (top row). Defaults to Sunday.</summary>
    [Parameter] public DayOfWeek WeekStart { get; set; } = DayOfWeek.Sunday;

    /// <summary>Side length of each day cell, in SVG units. Defaults to 12.</summary>
    [Parameter] public int CellSize { get; set; } = 12;

    /// <summary>Gap between cells, in SVG units. Defaults to 3.</summary>
    [Parameter] public int CellGap { get; set; } = 3;

    /// <summary>Corner radius of each cell. Defaults to 2.</summary>
    [Parameter] public double CellRadius { get; set; } = 2;

    /// <summary>Number of non-zero intensity levels (buckets). Defaults to 4.</summary>
    [Parameter] public int Levels { get; set; } = 4;

    /// <summary>Base (most-intense) cell color. Defaults to the theme's second palette color.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Show abbreviated month labels above the grid. Defaults to true.</summary>
    [Parameter] public bool ShowMonthLabels { get; set; } = true;

    /// <summary>Show weekday labels down the left. Defaults to true.</summary>
    [Parameter] public bool ShowWeekdayLabels { get; set; } = true;

    /// <summary>Show the "Less … More" intensity legend. Defaults to true.</summary>
    [Parameter] public bool ShowLegend { get; set; } = true;

    /// <summary>Show a themed hover tooltip with the date and value. Defaults to true.</summary>
    [Parameter] public bool ShowTooltip { get; set; } = true;

    /// <summary>Format string for the tooltip date. Defaults to <c>MMM d, yyyy</c>.</summary>
    [Parameter] public string DateFormat { get; set; } = "MMM d, yyyy";

    /// <summary>.NET numeric format string for the tooltip value (e.g. "N0").</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>Scale to the container width (SVG <c>width="100%"</c>). Defaults to true.</summary>
    [Parameter] public bool Responsive { get; set; } = true;

    /// <summary>Raised when a day cell is clicked.</summary>
    [Parameter] public EventCallback<CalendarPoint> OnDayClick { get; set; }

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private string BaseColor => Color ?? ResolvedTheme.GetColor(1);

    private DateOnly? _hovered;

    // ---- Layout ---------------------------------------------------------------------

    private sealed record Cell(DateOnly Date, int Col, int Row, decimal Value);

    private sealed record HeatLayout(
        List<Cell> Cells, int Weeks, decimal Max, DateOnly Start,
        List<(int Col, string Label)> Months, Dictionary<DateOnly, decimal> Values);

    private HeatLayout _layout = new([], 0, 0, default, [], []);

    // Recompute only when parameters change; hover re-renders reuse the cached grid.
    protected override void OnParametersSet() => _layout = Build();

    private DateOnly SnapToWeekStart(DateOnly d)
    {
        var delta = ((int)d.DayOfWeek - (int)WeekStart + 7) % 7;
        return d.AddDays(-delta);
    }

    private HeatLayout Build()
    {
        if (Data.Count == 0 && StartDate is null) return new([], 0, 0, default, [], []);

        var byDate = new Dictionary<DateOnly, decimal>();
        foreach (var p in Data)
            byDate[p.Date] = byDate.GetValueOrDefault(p.Date) + p.Value;

        var start = SnapToWeekStart(StartDate ?? Data.Min(d => d.Date));
        var end = EndDate ?? (Data.Count > 0 ? Data.Max(d => d.Date) : start);
        if (end < start) end = start;

        var cells = new List<Cell>();
        decimal max = 0;
        var totalDays = end.DayNumber - start.DayNumber;
        for (var i = 0; i <= totalDays; i++)
        {
            var date = start.AddDays(i);
            var v = byDate.GetValueOrDefault(date);
            if (v > max) max = v;
            cells.Add(new Cell(date, i / 7, i % 7, v));
        }

        var weeks = totalDays / 7 + 1;
        return new(cells, weeks, max, start, MonthLabels(start, weeks), byDate);
    }

    private static List<(int Col, string Label)> MonthLabels(DateOnly start, int weeks)
    {
        var labels = new List<(int Col, string Label)>();
        var prevMonth = -1;
        for (var col = 0; col < weeks; col++)
        {
            var top = start.AddDays(col * 7);
            if (top.Month == prevMonth) continue;
            prevMonth = top.Month;
            var name = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(top.Month);
            // Avoid a label crammed right next to the previous one (partial leading month).
            if (labels.Count > 0 && col - labels[^1].Col < 2)
                labels[^1] = (col, name);
            else
                labels.Add((col, name));
        }
        return labels;
    }

    // ---- Geometry -------------------------------------------------------------------

    private const double EmptyOpacity = 0.10;
    private const double MinLevelOpacity = 0.30;
    private const double MaxLevelOpacity = 1.0;

    private int Step => CellSize + CellGap;
    private int LeftPad => ShowWeekdayLabels ? 30 : 4;
    private int TopPad => ShowMonthLabels ? 18 : 4;
    private int LegendHeight => ShowLegend ? 22 : 0;
    private int GridWidth => Math.Max(0, _layout.Weeks * Step - CellGap);
    private int GridHeight => 7 * Step - CellGap;
    private int TotalWidth => LeftPad + GridWidth + 4;
    private int TotalHeight => TopPad + GridHeight + LegendHeight + 4;

    private double CellX(int col) => LeftPad + col * Step;
    private double CellY(int row) => TopPad + row * Step;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double d) => d.ToString("F1", Inv);

    private (string Fill, string Opacity) LevelStyle(int level)
    {
        if (level <= 0)
            return (ResolvedTheme.GridColor, EmptyOpacity.ToString("F2", Inv));
        var t = Levels <= 1 ? 1.0 : (level - 1) / (double)(Levels - 1);
        var op = MinLevelOpacity + (MaxLevelOpacity - MinLevelOpacity) * t;
        return (BaseColor, op.ToString("F2", Inv));
    }

    private (string Fill, string Opacity) CellStyle(decimal value)
    {
        if (value <= 0 || _layout.Max <= 0) return LevelStyle(0);
        var level = Math.Clamp((int)Math.Ceiling((double)(value / _layout.Max) * Levels), 1, Levels);
        return LevelStyle(level);
    }

    private string WeekdayLabel(int row) =>
        Inv.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)(((int)WeekStart + row) % 7));

    private string FormatValue(decimal v) =>
        ValueFormat is not null ? v.ToString(ValueFormat, Inv) : v.ToString("0.##", Inv);

    private string FormatDate(DateOnly d) => d.ToString(DateFormat, Inv);

    private void OnCellOver(DateOnly d) => _hovered = d;
    private void OnCellOut() => _hovered = null;

    private async Task OnCellClick(Cell cell)
    {
        if (OnDayClick.HasDelegate)
            await OnDayClick.InvokeAsync(new CalendarPoint { Date = cell.Date, Value = cell.Value });
    }

    private static string Escape(string? s) =>
        string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double TextWidth(string s) => s.Length * 6.0;
}
