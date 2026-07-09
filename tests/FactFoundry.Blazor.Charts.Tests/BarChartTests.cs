using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class BarChartTests : BunitContext
{
    private static List<ChartSeries> OneSeries() =>
        [new ChartSeries { Label = "Revenue", Values = [10, 20, 30] }];

    [Fact]
    public void Renders_SVG_Element()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.Width, 640)
            .Add(p => p.Height, 320));

        var svg = cut.Find("svg");
        Assert.Equal("640", svg.GetAttribute("width"));
        Assert.Equal("320", svg.GetAttribute("height"));
    }

    [Fact]
    public void Renders_Title_When_Provided()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Title, "Quarterly Revenue")
            .Add(p => p.Series, OneSeries()));

        var title = cut.Find("title");
        Assert.Equal("Quarterly Revenue", title.TextContent);
    }

    [Fact]
    public void Renders_One_Bar_Per_Value()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.ShowLegend, false));

        // With the legend off, the only <rect> elements are the bars.
        var rects = cut.FindAll("rect");
        Assert.Equal(3, rects.Count);
    }

    [Fact]
    public void Renders_Bar_Per_Series_Per_Category_When_Grouped()
    {
        var series = new List<ChartSeries>
        {
            new() { Label = "A", Values = [10, 20, 30] },
            new() { Label = "B", Values = [5, 15, 25] }
        };

        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, false));

        var rects = cut.FindAll("rect");
        Assert.Equal(6, rects.Count);
    }

    [Fact]
    public void Renders_Bars_When_Stacked()
    {
        var series = new List<ChartSeries>
        {
            new() { Label = "A", Values = [10, 20] },
            new() { Label = "B", Values = [5, 15] }
        };

        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.Stacked, true)
            .Add(p => p.ShowLegend, false));

        var rects = cut.FindAll("rect");
        Assert.Equal(4, rects.Count);
    }

    [Fact]
    public void Renders_Bars_When_Horizontal()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.Horizontal, true)
            .Add(p => p.ShowLegend, false));

        var rects = cut.FindAll("rect");
        Assert.Equal(3, rects.Count);
    }

    [Fact]
    public void Renders_Grid_Lines_When_ShowGrid_True()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.ShowGrid, true));

        var lines = cut.FindAll("line[stroke-dasharray]");
        Assert.NotEmpty(lines);
    }

    [Fact]
    public void Does_Not_Render_Grid_Lines_When_ShowGrid_False()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.ShowGrid, false));

        var lines = cut.FindAll("line[stroke-dasharray]");
        Assert.Empty(lines);
    }

    [Fact]
    public void Renders_Legend_When_ShowLegend_True()
    {
        var series = new List<ChartSeries>
        {
            new() { Label = "Series A", Values = [1, 2] },
            new() { Label = "Series B", Values = [3, 4] }
        };

        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, true));

        Assert.Contains("Series A", cut.Markup);
        Assert.Contains("Series B", cut.Markup);
    }

    [Fact]
    public void Responsive_Sets_Width_100_Percent_And_ViewBox()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, OneSeries())
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300)
            .Add(p => p.Responsive, true));

        var svg = cut.Find("svg");
        Assert.Equal("100%", svg.GetAttribute("width"));
        Assert.Equal("0 0 600 300", svg.GetAttribute("viewBox"));
        Assert.Null(svg.GetAttribute("height"));
    }

    [Fact]
    public void Empty_Series_Renders_Svg_Without_Bars()
    {
        var cut = Render<BarChart>(parameters => parameters
            .Add(p => p.Series, [])
            .Add(p => p.ShowLegend, false));

        Assert.NotNull(cut.Find("svg"));
        Assert.Empty(cut.FindAll("rect"));
    }
}
