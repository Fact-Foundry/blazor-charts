using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class BarListTests : BunitContext
{
    private static List<ChartSegment> Sample() =>
    [
        new() { Label = "Chrome", Value = 60 },
        new() { Label = "Safari", Value = 30 },
        new() { Label = "Firefox", Value = 10 }
    ];

    [Fact]
    public void Renders_Root_Element()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        Assert.NotNull(cut.Find(".ff-barlist"));
    }

    [Fact]
    public void Renders_One_Row_Per_Data_Item()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        var rows = cut.FindAll(".ff-barlist-row");
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void Ranks_Rows_Descending_By_Value()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "Small", Value = 10 },
            new() { Label = "Big", Value = 90 },
            new() { Label = "Medium", Value = 50 }
        };

        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, data));

        var keys = cut.FindAll(".ff-barlist-k");
        Assert.Contains("Big", keys[0].TextContent);
        Assert.Contains("Medium", keys[1].TextContent);
        Assert.Contains("Small", keys[2].TextContent);
    }

    [Fact]
    public void MaxItems_Limits_Row_Count()
    {
        var data = Enumerable.Range(1, 10)
            .Select(i => new ChartSegment { Label = $"Item {i}", Value = i })
            .ToList();

        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.MaxItems, 4));

        var rows = cut.FindAll(".ff-barlist-row");
        Assert.Equal(4, rows.Count);
    }

    [Fact]
    public void Largest_Row_Fill_Is_Full_Width()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        var fill = cut.FindAll(".ff-barlist-fill")[0];
        Assert.Contains("width:100%", fill.GetAttribute("style"));
    }

    [Fact]
    public void Fill_Width_Is_Proportional_To_Value()
    {
        // Second row (30) against a max of 60 → 50%.
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        var fill = cut.FindAll(".ff-barlist-fill")[1];
        Assert.Contains("width:50%", fill.GetAttribute("style"));
    }

    [Fact]
    public void Renders_Title_And_Caption()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample())
            .Add(p => p.Title, "Top browsers")
            .Add(p => p.Caption, "by sessions"));

        Assert.Equal("Top browsers", cut.Find(".ff-barlist-title").TextContent);
        Assert.Equal("by sessions", cut.Find(".ff-barlist-cap").TextContent);
    }

    [Fact]
    public void ShowShare_Appends_Percentage()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample())
            .Add(p => p.ShowShare, true));

        // 60 / 100 total → 60%.
        Assert.Contains("60%", cut.FindAll(".ff-barlist-n")[0].TextContent);
    }

    [Fact]
    public void No_Share_Percentage_By_Default()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        Assert.DoesNotContain("%", cut.Find(".ff-barlist-n").TextContent);
    }

    [Fact]
    public void ValueFormat_Is_Applied()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, [new ChartSegment { Label = "Views", Value = 4521 }])
            .Add(p => p.ValueFormat, "N0"));

        Assert.Contains("4,521", cut.Find(".ff-barlist-n").TextContent);
    }

    [Fact]
    public void ShowDot_Renders_Colored_Dots()
    {
        var data = new List<ChartSegment>
        {
            new() { Label = "Human", Value = 80, Color = "#22c55e" },
            new() { Label = "Bot", Value = 20, Color = "#f59e0b" }
        };

        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.ShowDot, true));

        var dots = cut.FindAll(".ff-barlist-dot");
        Assert.Equal(2, dots.Count);
        Assert.Contains("#22c55e", dots[0].GetAttribute("style"));
    }

    [Fact]
    public void No_Dots_By_Default()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample()));

        Assert.Empty(cut.FindAll(".ff-barlist-dot"));
    }

    [Fact]
    public void Renders_More_Link_When_Text_And_Href_Provided()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample())
            .Add(p => p.MoreText, "View all")
            .Add(p => p.MoreHref, "/all"));

        var link = cut.Find("a.ff-barlist-more");
        Assert.Equal("/all", link.GetAttribute("href"));
        Assert.Contains("View all", link.TextContent);
    }

    [Fact]
    public void No_More_Link_When_Href_Missing()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, Sample())
            .Add(p => p.MoreText, "View all"));

        Assert.Empty(cut.FindAll("a.ff-barlist-more"));
    }

    [Fact]
    public void Empty_Data_Renders_No_Rows()
    {
        var cut = Render<BarList>(parameters => parameters
            .Add(p => p.Data, []));

        Assert.Empty(cut.FindAll(".ff-barlist-row"));
    }
}
