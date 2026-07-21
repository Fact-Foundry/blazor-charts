using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A matrix (grid) chart — rows × columns of cells, each filled where a
/// <see cref="MatrixCell"/> connects them. Read a row for everything it touches, or a column
/// for everything that touches it. Rendered as pure Razor-to-SVG in the same idiom as the
/// other charts and themed through <c>ChartThemeProvider</c>. With uniform cell values it is
/// a categorical adjacency grid; with varying values the fill intensity turns it into a
/// heatmap. Cells are colored by their row (or column, via <see cref="ColorByColumn"/>).
/// </summary>
public partial class MatrixChart : ComponentBase
{
    /// <summary>The rows (top to bottom).</summary>
    [Parameter] public List<MatrixRow> Rows { get; set; } = [];

    /// <summary>The columns (left to right).</summary>
    [Parameter] public List<MatrixColumn> Columns { get; set; } = [];

    /// <summary>The filled intersections. Cells for unknown rows/columns are ignored; duplicates are summed.</summary>
    [Parameter] public List<MatrixCell> Cells { get; set; } = [];

    /// <summary>Side length of each cell, in SVG units. Defaults to 30.</summary>
    [Parameter] public int CellSize { get; set; } = 30;

    /// <summary>Gap between cells, in SVG units. Defaults to 4.</summary>
    [Parameter] public int CellGap { get; set; } = 4;

    /// <summary>Corner radius of each cell. Defaults to 5.</summary>
    [Parameter] public double CellRadius { get; set; } = 5;

    /// <summary>Base font size for labels, in SVG units. Defaults to 12.</summary>
    [Parameter] public double FontSize { get; set; } = 12;

    /// <summary>Color cells by their column instead of their row. Defaults to false.</summary>
    [Parameter] public bool ColorByColumn { get; set; }

    /// <summary>
    /// Value that maps to full intensity. When unset, the busiest cell defines it — so a
    /// uniform-value grid renders every cell at full color (a membership matrix).
    /// </summary>
    [Parameter] public double? MaxValue { get; set; }

    /// <summary>Print each cell's value inside it. Defaults to false.</summary>
    [Parameter] public bool ShowValues { get; set; }

    /// <summary>.NET numeric format for values and totals (e.g. "N0"). Defaults to a compact form.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>Show a per-row total column on the right (sum of the row's cell values). Defaults to false.</summary>
    [Parameter] public bool ShowRowTotals { get; set; }

    /// <summary>Show a per-column total row along the bottom. Defaults to false.</summary>
    [Parameter] public bool ShowColumnTotals { get; set; }

    /// <summary>Show a themed hover tooltip and row/column crosshair. Defaults to true.</summary>
    [Parameter] public bool ShowTooltip { get; set; } = true;

    /// <summary>Fill the container width (SVG <c>width="100%"</c>), capped at the grid's natural size. Defaults to true.</summary>
    [Parameter] public bool Responsive { get; set; } = true;

    /// <summary>Raised when a filled cell is clicked.</summary>
    [Parameter] public EventCallback<MatrixCell> OnCellClick { get; set; }

    /// <summary>Optional accessible name; falls back to <see cref="Title"/> then "Matrix chart".</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Accessible description read by screen readers, emitted as the SVG <c>&lt;desc&gt;</c>. Auto-generated when unset.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private readonly string _a11yId = "ffc-" + Guid.NewGuid().ToString("N")[..8];
    private string TitleId => _a11yId + "t";
    private string DescId => _a11yId + "d";
    private string AccessibleName => string.IsNullOrWhiteSpace(Title) ? "Matrix chart" : Title!;
    private string AccessibleDescription => Description ?? BuildAccessibleDescription();

    private string BuildAccessibleDescription() =>
        _layout.Rows.Count == 0
            ? "Matrix chart with no data."
            : $"Matrix chart of {_layout.Rows.Count} rows and {_layout.Cols.Count} columns " +
              $"with {_layout.Cells.Count} filled cells.";

    private int? _hoverRow;
    private int? _hoverCol;

    // ---- Layout ---------------------------------------------------------------------

    private sealed record MRow(string Id, string Label, string Color, double Total);
    private sealed record MCol(string Id, string Label, string Color, double Total);
    private sealed record MCell(int R, int C, double Value, string Color, MatrixCell Source);

    private sealed record MatrixLayout(
        List<MRow> Rows, List<MCol> Cols, List<MCell> Cells,
        double Max, double[,] Grid, MatrixCell?[,] SourceGrid);

    private MatrixLayout _layout = new([], [], [], 0, new double[0, 0], new MatrixCell?[0, 0]);

    protected override void OnParametersSet() => _layout = BuildLayout();

    private MatrixLayout BuildLayout()
    {
        if (Rows.Count == 0 || Columns.Count == 0)
            return new([], [], [], 0, new double[0, 0], new MatrixCell?[0, 0]);

        var rowIndex = new Dictionary<string, int>();
        var rows = new List<MRow>();
        for (var i = 0; i < Rows.Count; i++)
        {
            var r = Rows[i];
            if (string.IsNullOrEmpty(r.Id) || rowIndex.ContainsKey(r.Id)) continue;
            rowIndex[r.Id] = rows.Count;
            rows.Add(new MRow(r.Id, string.IsNullOrEmpty(r.Label) ? r.Id : r.Label!, r.Color ?? ResolvedTheme.GetColor(rows.Count), 0));
        }

        var colIndex = new Dictionary<string, int>();
        var cols = new List<MCol>();
        for (var i = 0; i < Columns.Count; i++)
        {
            var c = Columns[i];
            if (string.IsNullOrEmpty(c.Id) || colIndex.ContainsKey(c.Id)) continue;
            colIndex[c.Id] = cols.Count;
            cols.Add(new MCol(c.Id, string.IsNullOrEmpty(c.Label) ? c.Id : c.Label!, c.Color ?? ResolvedTheme.GetColor(cols.Count), 0));
        }

        var grid = new double[rows.Count, cols.Count];
        var srcGrid = new MatrixCell?[rows.Count, cols.Count];
        var cells = new List<MCell>();
        var rowTotals = new double[rows.Count];
        var colTotals = new double[cols.Count];
        double max = 0;

        foreach (var cell in Cells)
        {
            if (cell.Value <= 0) continue;
            if (!rowIndex.TryGetValue(cell.Row, out var r) || !colIndex.TryGetValue(cell.Column, out var c)) continue;
            grid[r, c] += cell.Value;
            srcGrid[r, c] = cell;
            rowTotals[r] += cell.Value;
            colTotals[c] += cell.Value;
        }

        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < cols.Count; c++)
            {
                var v = grid[r, c];
                if (v <= 0) continue;
                if (v > max) max = v;
                var color = srcGrid[r, c]?.Color ?? (ColorByColumn ? cols[c].Color : rows[r].Color);
                cells.Add(new MCell(r, c, v, color, srcGrid[r, c]!));
            }

        for (var r = 0; r < rows.Count; r++) rows[r] = rows[r] with { Total = rowTotals[r] };
        for (var c = 0; c < cols.Count; c++) cols[c] = cols[c] with { Total = colTotals[c] };

        return new MatrixLayout(rows, cols, cells, MaxValue ?? max, grid, srcGrid);
    }

    // ---- Geometry -------------------------------------------------------------------

    private const double EmptyOpacity = 0.10;
    private const double MinFillOpacity = 0.35;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double d) => d.ToString("F1", Inv);
    private double LabelFont => FontSize;
    private double TextWidth(string s) => s.Length * LabelFont * 0.58;

    private int Step => CellSize + CellGap;

    private double RowLabelWidth =>
        _layout.Rows.Select(r => TextWidth(r.Label)).DefaultIfEmpty(0).Max() + 12;

    private double ColLabelHeight =>
        Math.Min(150, _layout.Cols.Select(c => TextWidth(c.Label)).DefaultIfEmpty(0).Max()) + 8;

    private double TotalsColWidth => ShowRowTotals
        ? Math.Max(TextWidth("Total"), _layout.Rows.Select(r => TextWidth(FormatValue(r.Total))).DefaultIfEmpty(0).Max()) + 18
        : 0;
    private double TotalsRowHeight => ShowColumnTotals ? LabelFont + 10 : 0;

    private double LeftPad => RowLabelWidth;
    private double TopPad => ColLabelHeight;
    private double GridWidth => Math.Max(0, _layout.Cols.Count * Step - CellGap);
    private double GridHeight => Math.Max(0, _layout.Rows.Count * Step - CellGap);
    private double TotalWidth => LeftPad + GridWidth + TotalsColWidth + 4;
    private double TotalHeight => TopPad + GridHeight + TotalsRowHeight + 4;

    private double CellX(int col) => LeftPad + col * Step;
    private double CellY(int row) => TopPad + row * Step;

    private string FormatValue(double v) =>
        ValueFormat is not null ? v.ToString(ValueFormat, Inv) : v.ToString("0.##", Inv);

    private (string Fill, string Opacity) CellStyle(MCell cell)
    {
        var op = _layout.Max <= 0 ? 1.0 : MinFillOpacity + (1 - MinFillOpacity) * (cell.Value / _layout.Max);
        return (cell.Color, op.ToString("F2", Inv));
    }

    private void OnCellOver(int r, int c) { _hoverRow = r; _hoverCol = c; }
    private void OnCellOut() { _hoverRow = null; _hoverCol = null; }

    private async Task CellClick(MCell cell)
    {
        if (OnCellClick.HasDelegate)
            await OnCellClick.InvokeAsync(cell.Source);
    }

    private static string Escape(string? s) =>
        string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
