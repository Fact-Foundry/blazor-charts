using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Geo;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class WorldMapChartTests : BunitContext
{
    private static string FirstCountryCode => WorldGeometry.CountryPaths.Keys.First();

    [Fact]
    public void Renders_SVG_Element()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Width, 800)
            .Add(p => p.Height, 400));

        var svg = cut.Find("svg");
        Assert.Equal("800", svg.GetAttribute("width"));
        Assert.Equal("400", svg.GetAttribute("height"));
    }

    [Fact]
    public void Renders_A_Path_For_Every_Country()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, []));

        var paths = cut.FindAll("path");
        Assert.Equal(WorldGeometry.CountryPaths.Count, paths.Count);
        Assert.NotEmpty(paths);
    }

    [Fact]
    public void Renders_Title_When_Provided()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Title, "Global Traffic"));

        var title = cut.Find("title");
        Assert.Equal("Global Traffic", title.TextContent);
    }

    [Fact]
    public void Countries_Without_Data_Use_NoDataColor()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, [])
            .Add(p => p.NoDataColor, "#123456"));

        var paths = cut.FindAll("path");
        Assert.All(paths, p => Assert.Equal("#123456", p.GetAttribute("fill")));
    }

    [Fact]
    public void Country_With_Data_Gets_A_Non_NoData_Fill()
    {
        var data = new List<MapDataPoint>
        {
            new() { CountryCode = FirstCountryCode, Value = 100 }
        };

        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.NoDataColor, "#e5e7eb"));

        var paths = cut.FindAll("path");
        var withData = paths.Count(p => p.GetAttribute("fill") != "#e5e7eb");
        Assert.Equal(1, withData);
    }

    [Fact]
    public void CountryCode_Lookup_Is_Case_Insensitive()
    {
        var data = new List<MapDataPoint>
        {
            new() { CountryCode = FirstCountryCode.ToLowerInvariant(), Value = 100 }
        };

        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.NoDataColor, "#e5e7eb"));

        var withData = cut.FindAll("path").Count(p => p.GetAttribute("fill") != "#e5e7eb");
        Assert.Equal(1, withData);
    }

    [Fact]
    public void Single_Color_Scale_Applies_That_Color_To_Data_Countries()
    {
        var data = new List<MapDataPoint>
        {
            new() { CountryCode = FirstCountryCode, Value = 50 }
        };

        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ColorScale, ["#ff0000"]));

        Assert.Equal(1, cut.FindAll("path").Count(p => p.GetAttribute("fill") == "#ff0000"));
    }

    [Fact]
    public void Renders_Legend_Gradient_When_Data_And_ShowLegend()
    {
        var data = new List<MapDataPoint>
        {
            new() { CountryCode = FirstCountryCode, Value = 100 }
        };

        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowLegend, true));

        Assert.NotEmpty(cut.FindAll("linearGradient"));
    }

    [Fact]
    public void No_Legend_When_Data_Empty()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, [])
            .Add(p => p.ShowLegend, true));

        Assert.Empty(cut.FindAll("linearGradient"));
    }

    [Fact]
    public void No_Legend_When_ShowLegend_False()
    {
        var data = new List<MapDataPoint>
        {
            new() { CountryCode = FirstCountryCode, Value = 100 }
        };

        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowLegend, false));

        Assert.Empty(cut.FindAll("linearGradient"));
    }

    [Fact]
    public void Responsive_Sets_Width_100_Percent_And_ViewBox()
    {
        var cut = Render<WorldMapChart>(parameters => parameters
            .Add(p => p.Width, 900)
            .Add(p => p.Height, 450)
            .Add(p => p.Responsive, true));

        var svg = cut.Find("svg");
        Assert.Equal("100%", svg.GetAttribute("width"));
        Assert.Equal("0 0 900 450", svg.GetAttribute("viewBox"));
        Assert.Null(svg.GetAttribute("height"));
    }
}
