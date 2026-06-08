using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class DonutChartTests : BunitContext
{
    [Fact]
    public void Renders_SVG_Element()
    {
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Data, [new ChartSegment { Label = "A", Value = 100 }])
            .Add(p => p.Width, 300)
            .Add(p => p.Height, 300));

        var svg = cut.Find("svg");
        Assert.Equal("300", svg.GetAttribute("width"));
    }

    [Fact]
    public void Renders_Path_For_Each_Segment()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "A", Value = 50 },
            new() { Label = "B", Value = 30 },
            new() { Label = "C", Value = 20 }
        };

        var cut = Render<DonutChart>(parameters => parameters
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
            new() { Label = "E", Value = 20 }
        };

        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.MaxSegments, 3));

        var paths = cut.FindAll("path");
        Assert.Equal(3, paths.Count);
        Assert.Contains("Other", cut.Markup);
    }

    [Fact]
    public void Shows_All_Segments_When_MaxSegments_Zero()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "A", Value = 50 },
            new() { Label = "B", Value = 30 },
            new() { Label = "C", Value = 20 },
            new() { Label = "D", Value = 10 },
            new() { Label = "E", Value = 5 }
        };

        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.MaxSegments, 0));

        var paths = cut.FindAll("path");
        Assert.Equal(5, paths.Count);
    }

    [Fact]
    public void Renders_Center_Label_When_Provided()
    {
        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Data, [new ChartSegment { Label = "A", Value = 100 }])
            .Add(p => p.CenterLabel, "Total"));

        Assert.Contains("Total", cut.Markup);
    }

    [Fact]
    public void Renders_Legend_When_ShowLegend_True()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "Chrome", Value = 60 },
            new() { Label = "Firefox", Value = 40 }
        };

        var cut = Render<DonutChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowLegend, true));

        Assert.Contains("Chrome", cut.Markup);
        Assert.Contains("Firefox", cut.Markup);
    }
}
