using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class PieChartTests : BunitContext
{
    [Fact]
    public void Renders_SVG_Element()
    {
        var cut = Render<PieChart>(parameters => parameters
            .Add(p => p.Data, [new ChartSegment { Label = "A", Value = 100 }])
            .Add(p => p.Width, 400)
            .Add(p => p.Height, 300));

        var svg = cut.Find("svg");
        Assert.Equal("400", svg.GetAttribute("width"));
        Assert.Equal("300", svg.GetAttribute("height"));
    }

    [Fact]
    public void Renders_Path_For_Each_Segment()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "A", Value = 40 },
            new() { Label = "B", Value = 35 },
            new() { Label = "C", Value = 25 }
        };

        var cut = Render<PieChart>(parameters => parameters
            .Add(p => p.Data, data));

        var paths = cut.FindAll("path");
        Assert.Equal(3, paths.Count);
    }

    [Fact]
    public void MaxSegments_Groups_Excess_Into_Other()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "A", Value = 100 },
            new() { Label = "B", Value = 80 },
            new() { Label = "C", Value = 60 },
            new() { Label = "D", Value = 40 },
            new() { Label = "E", Value = 20 },
            new() { Label = "F", Value = 10 }
        };

        var cut = Render<PieChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.MaxSegments, 4));

        var paths = cut.FindAll("path");
        Assert.Equal(4, paths.Count);
        Assert.Contains("Other", cut.Markup);
    }

    [Fact]
    public void Renders_Legend_When_ShowLegend_True()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "Windows", Value = 60 },
            new() { Label = "macOS", Value = 40 }
        };

        var cut = Render<PieChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowLegend, true));

        Assert.Contains("Windows", cut.Markup);
        Assert.Contains("macOS", cut.Markup);
    }

    [Fact]
    public void Single_Segment_Renders_Full_Circle()
    {
        var cut = Render<PieChart>(parameters => parameters
            .Add(p => p.Data, [new ChartSegment { Label = "Only", Value = 100 }]));

        var path = cut.Find("path");
        var d = path.GetAttribute("d")!;
        Assert.Equal(2, d.Split('A').Length - 1);
    }
}
