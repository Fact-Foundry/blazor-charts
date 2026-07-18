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

    [Fact]
    public void Responsive_Sets_Width_100_Percent_And_ViewBox()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }])
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300)
            .Add(p => p.Responsive, true));

        var svg = cut.Find("svg");
        Assert.Equal("100%", svg.GetAttribute("width"));
        Assert.Equal("0 0 600 300", svg.GetAttribute("viewBox"));
        Assert.Equal("xMidYMid meet", svg.GetAttribute("preserveAspectRatio"));
        Assert.Null(svg.GetAttribute("height"));
    }

    [Fact]
    public void Non_Responsive_Uses_Fixed_Dimensions()
    {
        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = [1, 2, 3] }])
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300)
            .Add(p => p.Responsive, false));

        var svg = cut.Find("svg");
        Assert.Equal("600", svg.GetAttribute("width"));
        Assert.Equal("300", svg.GetAttribute("height"));
        Assert.Null(svg.GetAttribute("viewBox"));
        Assert.Null(svg.GetAttribute("preserveAspectRatio"));
    }

    [Fact]
    public void Thins_Labels_When_Too_Many_Points()
    {
        var labels = Enumerable.Range(1, 30).Select(i => $"2024-01-{i:D2}").ToList();
        var values = Enumerable.Range(1, 30).Select(i => (decimal)i).ToList();

        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = values }])
            .Add(p => p.XAxisLabels, labels)
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300));

        var labelCount = cut.Markup.Split("2024-01-").Length - 1;
        Assert.True(labelCount < 30, $"Expected fewer than 30 labels rendered, got {labelCount}");
        Assert.True(labelCount >= 5, $"Expected at least 5 labels rendered, got {labelCount}");
    }

    [Fact]
    public void MaxXAxisLabels_Limits_Label_Count()
    {
        var labels = Enumerable.Range(1, 20).Select(i => $"Day{i}").ToList();
        var values = Enumerable.Range(1, 20).Select(i => (decimal)i).ToList();

        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = values }])
            .Add(p => p.XAxisLabels, labels)
            .Add(p => p.MaxXAxisLabels, 5)
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300));

        // Count rendered x-axis label <text> elements (the <desc> is not a <text>).
        var labelCount = cut.FindAll("text").Count(t => t.TextContent.StartsWith("Day"));
        Assert.True(labelCount <= 5, $"Expected at most 5 labels, got {labelCount}");
    }

    [Fact]
    public void Last_Label_Always_Rendered_When_Thinning()
    {
        var labels = Enumerable.Range(1, 30).Select(i => $"2024-01-{i:D2}").ToList();
        var values = Enumerable.Range(1, 30).Select(i => (decimal)i).ToList();

        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, [new ChartSeries { Label = "Test", Values = values }])
            .Add(p => p.XAxisLabels, labels)
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300));

        Assert.Contains("2024-01-30", cut.Markup);
    }

    [Fact]
    public void Legend_Wraps_With_Many_Series()
    {
        var series = Enumerable.Range(1, 8)
            .Select(i => new ChartSeries { Label = $"Series {i}", Values = [i, i + 1, i + 2] })
            .ToList();

        var cut = Render<LineChart>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.ShowLegend, true)
            .Add(p => p.Width, 600)
            .Add(p => p.Height, 300));

        for (var i = 1; i <= 8; i++)
            Assert.Contains($"Series {i}", cut.Markup);
    }
}
