using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class MatrixChartTests : BunitContext
{
    private static List<MatrixRow> Rows(params string[] ids) =>
        ids.Select(id => new MatrixRow { Id = id, Label = id }).ToList();

    private static List<MatrixColumn> Cols(params string[] ids) =>
        ids.Select(id => new MatrixColumn { Id = id, Label = id }).ToList();

    private static MatrixCell Cell(string r, string c, double v = 1) =>
        new() { Row = r, Column = c, Value = v };

    private static double Attr(IElement e, string name) =>
        double.Parse(e.GetAttribute(name)!, CultureInfo.InvariantCulture);

    // A 2x2 grid with two filled cells: (r1,c1) and (r2,c2).
    private static IRenderedComponent<MatrixChart> TwoByTwo(BunitContext ctx) =>
        ctx.Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1", "r2"))
            .Add(x => x.Columns, Cols("c1", "c2"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1"), Cell("r2", "c2") }));

    [Fact]
    public void Renders_Root_Svg()
    {
        var cut = TwoByTwo(this);
        Assert.NotNull(cut.Find("svg.ff-matrix"));
    }

    [Fact]
    public void No_Rows_Or_No_Columns_Renders_Nothing()
    {
        var noCols = Render<MatrixChart>(p => p.Add(x => x.Rows, Rows("r1")));
        Assert.Empty(noCols.FindAll("svg"));

        var noRows = Render<MatrixChart>(p => p.Add(x => x.Columns, Cols("c1")));
        Assert.Empty(noRows.FindAll("svg"));
    }

    [Fact]
    public void Draws_A_Rect_For_Every_Cell_Filled_And_Empty()
    {
        // 2x2 grid → 4 cell rects regardless of how many are filled.
        var cut = TwoByTwo(this);
        Assert.Equal(4, cut.FindAll("rect").Count);
    }

    [Fact]
    public void Renders_Row_And_Column_Labels()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, new List<MatrixRow> { new() { Id = "sales", Label = "Sales" } })
            .Add(x => x.Columns, new List<MatrixColumn> { new() { Id = "order", Label = "Order" } })
            .Add(x => x.Cells, new List<MatrixCell> { Cell("sales", "order") }));

        Assert.Contains("Sales", cut.Markup);
        Assert.Contains("Order", cut.Markup);
    }

    [Fact]
    public void Cells_For_Unknown_Rows_Or_Columns_Are_Ignored()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1"))
            .Add(x => x.Columns, Cols("c1"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1"), Cell("ghost", "c1"), Cell("r1", "void") }));

        // 1x1 grid → exactly one cell rect; the two bad cells don't add rows/cols.
        Assert.Single(cut.FindAll("rect"));
        Assert.Contains("1 filled cells", cut.Find("desc").TextContent);
    }

    [Fact]
    public void Duplicate_Cells_Are_Summed()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1"))
            .Add(x => x.Columns, Cols("c1"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1", 2), Cell("r1", "c1", 3) })
            .Add(x => x.ShowValues, true));

        Assert.Contains(">5<", cut.Markup);
    }

    [Fact]
    public void Fill_Opacity_Scales_With_Value()
    {
        // Two filled cells, values 10 and 2, max=10 → first near full, second dimmer.
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1", "r2"))
            .Add(x => x.Columns, Cols("c1"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1", 10), Cell("r2", "c1", 2) }));

        // Filled cells are the rects carrying a fill-opacity > the empty-cell 0.10.
        var filled = cut.FindAll("rect")
            .Select(e => Attr(e, "fill-opacity"))
            .Where(o => o > 0.11)
            .OrderByDescending(o => o)
            .ToList();

        Assert.Equal(2, filled.Count);
        Assert.True(filled[0] > filled[1], "higher-value cell should be more opaque");
    }

    [Fact]
    public void Row_And_Column_Totals_Render_When_Enabled()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1", "r2"))
            .Add(x => x.Columns, Cols("c1", "c2"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1", 3), Cell("r1", "c2", 4), Cell("r2", "c1", 5) })
            .Add(x => x.ShowRowTotals, true)
            .Add(x => x.ShowColumnTotals, true));

        // Row r1 total = 7, column c1 total = 8.
        Assert.Contains(">7<", cut.Markup);
        Assert.Contains(">8<", cut.Markup);
    }

    [Fact]
    public void Hover_A_Filled_Cell_Shows_A_Tooltip()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, new List<MatrixRow> { new() { Id = "sales", Label = "Sales" } })
            .Add(x => x.Columns, new List<MatrixColumn> { new() { Id = "order", Label = "Order" } })
            .Add(x => x.Cells, new List<MatrixCell> { Cell("sales", "order", 4) }));

        Assert.Empty(cut.FindAll("g"));
        cut.FindAll("rect")[0].MouseOver();
        Assert.NotEmpty(cut.FindAll("g"));
        Assert.Contains("Sales × Order", cut.Markup);
    }

    [Fact]
    public void OnCellClick_Fires_With_The_Clicked_Cell()
    {
        MatrixCell? clicked = null;
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1"))
            .Add(x => x.Columns, Cols("c1"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1", 9) })
            .Add(x => x.OnCellClick, c => clicked = c));

        // The single filled cell is the last rect (empties drawn first, filled after).
        cut.FindAll("rect").Last().Click();
        Assert.NotNull(clicked);
        Assert.Equal("r1", clicked!.Row);
    }

    [Fact]
    public void Labels_Are_Html_Escaped()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, new List<MatrixRow> { new() { Id = "r", Label = "<b>x</b>" } })
            .Add(x => x.Columns, Cols("c"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r", "c") }));

        Assert.DoesNotContain("<b>x</b>", cut.Markup);
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", cut.Markup);
    }

    [Fact]
    public void Responsive_Caps_Width_Nonresponsive_Is_Fixed()
    {
        var responsive = TwoByTwo(this);
        var svg = responsive.Find("svg");
        Assert.Equal("100%", svg.GetAttribute("width"));
        Assert.Contains("max-width:", svg.GetAttribute("style"));

        var fixedW = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1", "r2"))
            .Add(x => x.Columns, Cols("c1", "c2"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1") })
            .Add(x => x.Responsive, false));
        Assert.DoesNotContain("max-width", fixedW.Find("svg").GetAttribute("style"));
        Assert.NotEqual("100%", fixedW.Find("svg").GetAttribute("width"));
    }

    [Fact]
    public void Accessible_Name_Desc_And_Role()
    {
        var cut = TwoByTwo(this);
        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        var titleId = cut.Find("title").GetAttribute("id");
        var descId = cut.Find("desc").GetAttribute("id");
        Assert.Equal($"{titleId} {descId}", svg.GetAttribute("aria-labelledby"));
        Assert.Equal("Matrix chart", cut.Find("title").TextContent);
        Assert.Contains("2 rows and 2 columns", cut.Find("desc").TextContent);
    }

    [Fact]
    public void Title_Parameter_Becomes_Accessible_Name()
    {
        var cut = Render<MatrixChart>(p => p
            .Add(x => x.Rows, Rows("r1"))
            .Add(x => x.Columns, Cols("c1"))
            .Add(x => x.Cells, new List<MatrixCell> { Cell("r1", "c1") })
            .Add(x => x.Title, "Model bindings"));

        Assert.Equal("Model bindings", cut.Find("title").TextContent);
    }
}
