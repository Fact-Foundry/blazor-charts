using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

/// <summary>
/// Every SVG chart exposes an accessible name (<c>&lt;title&gt;</c>) and description
/// (<c>&lt;desc&gt;</c>) associated via <c>role="img"</c> + <c>aria-labelledby</c>.
/// </summary>
public class AccessibilityTests : BunitContext
{
    [Fact]
    public void LineChart_Has_Role_Title_Desc_And_AriaLabelledby()
    {
        var cut = Render<LineChart>(p => p
            .Add(x => x.Series, [new ChartSeries { Label = "Sales", Values = [1, 2, 3] }])
            .Add(x => x.XAxisLabels, ["Jan", "Feb", "Mar"]));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));

        var titleId = cut.Find("title").GetAttribute("id");
        var descId = cut.Find("desc").GetAttribute("id");
        Assert.Equal($"{titleId} {descId}", svg.GetAttribute("aria-labelledby"));

        // Auto-description summarizes the data.
        var desc = cut.Find("desc").TextContent;
        Assert.Contains("1 series", desc);
        Assert.Contains("Sales", desc);
        Assert.Contains("Jan", desc);
        Assert.Contains("Mar", desc);
    }

    [Fact]
    public void Title_Parameter_Becomes_The_Accessible_Name()
    {
        var cut = Render<LineChart>(p => p
            .Add(x => x.Title, "Quarterly revenue")
            .Add(x => x.Series, [new ChartSeries { Label = "R", Values = [1, 2] }]));

        Assert.Equal("Quarterly revenue", cut.Find("title").TextContent);
    }

    [Fact]
    public void Untitled_Chart_Falls_Back_To_A_Type_Name()
    {
        var cut = Render<LineChart>(p => p
            .Add(x => x.Series, [new ChartSeries { Label = "R", Values = [1, 2] }]));

        Assert.Equal("Line chart", cut.Find("title").TextContent);
    }

    [Fact]
    public void Explicit_Description_Overrides_The_Auto_Description()
    {
        var cut = Render<DonutChart>(p => p
            .Add(x => x.Data, [new ChartSegment { Label = "A", Value = 1 }])
            .Add(x => x.Description, "Custom screen-reader text"));

        Assert.Equal("Custom screen-reader text", cut.Find("desc").TextContent);
    }

    [Fact]
    public void Description_Is_Html_Escaped()
    {
        var cut = Render<PieChart>(p => p
            .Add(x => x.Data, [new ChartSegment { Label = "A", Value = 1 }])
            .Add(x => x.Description, "<script>alert(1)</script>"));

        Assert.DoesNotContain("<script>", cut.Markup);
        Assert.Contains("&lt;script&gt;", cut.Markup);
    }

    [Fact]
    public void PieChart_Auto_Description_Names_Largest_Segment_And_Share()
    {
        var cut = Render<PieChart>(p => p.Add(x => x.Data,
        [
            new ChartSegment { Label = "Chrome", Value = 60 },
            new ChartSegment { Label = "Safari", Value = 40 }
        ]));

        var desc = cut.Find("desc").TextContent;
        Assert.Contains("2 segments", desc);
        Assert.Contains("Chrome", desc);
        Assert.Contains("60%", desc);
    }

    [Fact]
    public void CommitGraph_Auto_Description_Counts_Commits()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits,
        [
            new CommitNode { Id = "b", ParentIds = ["a"] },
            new CommitNode { Id = "a", ParentIds = [] }
        ]));

        Assert.Contains("2 commits", cut.Find("desc").TextContent);
    }

    [Fact]
    public void CalendarHeatmap_Auto_Description_Names_Range_And_Busiest_Day()
    {
        var cut = Render<CalendarHeatmap>(p => p.Add(x => x.Data,
        [
            new CalendarPoint { Date = new DateOnly(2026, 1, 4), Value = 2 },
            new CalendarPoint { Date = new DateOnly(2026, 1, 6), Value = 9 }
        ]));

        var desc = cut.Find("desc").TextContent;
        Assert.Contains("Jan 4, 2026", desc);
        Assert.Contains("Busiest day Jan 6, 2026", desc);
    }

    [Fact]
    public void Titleless_Components_Get_A_Sensible_Default_Name()
    {
        var spark = Render<Sparkline>(p => p.Add(x => x.Values, [1m, 2m, 3m]));
        Assert.Equal("Sparkline", spark.Find("title").TextContent);

        var graph = Render<CommitGraph>(p => p.Add(x => x.Commits,
            [new CommitNode { Id = "a", ParentIds = [] }]));
        Assert.Equal("Commit graph", graph.Find("title").TextContent);

        var cal = Render<CalendarHeatmap>(p => p.Add(x => x.Data,
            [new CalendarPoint { Date = new DateOnly(2026, 1, 1), Value = 1 }]));
        Assert.Equal("Calendar heatmap", cal.Find("title").TextContent);
    }
}
