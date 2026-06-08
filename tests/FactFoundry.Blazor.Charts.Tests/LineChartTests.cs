using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class LineChartTests : BunitContext
{
    [Fact]
    public void Renders_SVG_Element()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }])
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300));

        var svg = cut.Find("svg");
        Assert.Equal("600", svg.GetAttribute("width"));
        Assert.Equal("300", svg.GetAttribute("height"));
    }

    [Fact]
    public void Renders_Title_When_Provided()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Title, "My Chart")
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }]));

        var title = cut.Find("title");
        Assert.Equal("My Chart", title.TextContent);
    }

    [Fact]
    public void Renders_Path_For_Each_Series()
    {
        var series = new List<ChartSeries>
        {
            new() { Label = "A", Values = [10, 20, 30] },
            new() { Label = "B", Values = [5, 15, 25] }
        };

        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, series));

        var paths = cut.FindAll("path");
        Assert.True(paths.Count >= 2);
    }

    [Fact]
    public void Renders_Grid_Lines_When_ShowGrid_True()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }])
            .Add(p => p.ShowGrid, true));

        var lines = cut.FindAll("line[stroke-dasharray]");
        Assert.NotEmpty(lines);
    }

    [Fact]
    public void Does_Not_Render_Grid_Lines_When_ShowGrid_False()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }])
            .Add(p => p.ShowGrid, false));

        var lines = cut.FindAll("line[stroke-dasharray]");
        Assert.Empty(lines);
    }

    [Fact]
    public void Renders_Data_Points_As_Circles()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [10, 20, 30, 40] }]));

        var circles = cut.FindAll("circle");
        Assert.Equal(4, circles.Count);
    }

    [Fact]
    public void SmoothLines_Uses_Cubic_Bezier_Commands()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [10, 20, 30, 40] }])
            .Add(p => p.SmoothLines, true));

        var path = cut.Find("path[stroke]");
        Assert.Contains("C", path.GetAttribute("d"));
    }

    [Fact]
    public void StraightLines_Uses_LineTo_Commands()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [10, 20, 30, 40] }])
            .Add(p => p.SmoothLines, false));

        var path = cut.Find("path[stroke]");
        Assert.Contains("L", path.GetAttribute("d"));
    }

    [Fact]
    public void Renders_Legend_When_ShowLegend_True()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Series A", Values = [1, 2] }])
            .Add(p => p.ShowLegend, true));

        Assert.Contains("Series A", cut.Markup);
    }
}
