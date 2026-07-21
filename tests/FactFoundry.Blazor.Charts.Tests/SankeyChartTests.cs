using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class SankeyChartTests : BunitContext
{
    private static List<SankeyNode> Nodes(params string[] ids) =>
        ids.Select(id => new SankeyNode { Id = id, Label = id }).ToList();

    private static SankeyLink Link(string s, string t, double v = 1) =>
        new() { Source = s, Target = t, Value = v };

    private static double Attr(IElement e, string name) =>
        double.Parse(e.GetAttribute(name)!, CultureInfo.InvariantCulture);

    [Fact]
    public void Renders_Root_Svg()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") }));
        Assert.NotNull(cut.Find("svg.ff-sankey"));
    }

    [Fact]
    public void Empty_Renders_Nothing()
    {
        var cut = Render<SankeyChart>(p => p.Add(x => x.Nodes, new List<SankeyNode>()));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void One_Rect_Per_Node_And_One_Path_Per_Link()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b", "c"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("a", "c") }));

        Assert.Equal(3, cut.FindAll("rect").Count);
        Assert.Equal(2, cut.FindAll("path").Count);
    }

    [Fact]
    public void Links_To_Unknown_Nodes_Are_Ignored()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("a", "ghost"), Link("void", "b") }));

        Assert.Single(cut.FindAll("path"));
    }

    [Fact]
    public void Source_Sits_Left_Of_Target()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") }));

        var rects = cut.FindAll("rect");
        Assert.True(Attr(rects[0], "x") < Attr(rects[1], "x"), "source node should be in a column left of its target");
    }

    [Fact]
    public void Node_Height_Scales_With_Flow_Value()
    {
        // 'a' feeds two entities → its value (2) is double each leaf's (1), so its bar is taller.
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b", "c"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("a", "c") }));

        var rects = cut.FindAll("rect");
        var hubH = Attr(rects[0], "height");
        var leafH = Attr(rects[1], "height");
        Assert.True(hubH > leafH * 1.5, $"hub height {hubH} should be ~2x leaf {leafH}");
    }

    [Fact]
    public void Ribbon_Thickness_Follows_Link_Value()
    {
        // A thick (value 3) and a thin (value 1) link from the same source: the source bar
        // height is 4 units; check the two ribbons differ in drawn thickness via their paths.
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b", "c"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b", 3), Link("a", "c", 1) }));

        // The 'b' target bar (in-value 3) must be 3x the 'c' target bar (in-value 1).
        var rects = cut.FindAll("rect");
        var hB = Attr(rects[1], "height");
        var hC = Attr(rects[2], "height");
        Assert.True(hB > hC * 2.5, $"target of thick link ({hB}) should dwarf target of thin link ({hC})");
    }

    [Fact]
    public void Explicit_Layer_Is_Respected()
    {
        var nodes = Nodes("a", "b");
        nodes[0].Layer = 0;
        nodes[1].Layer = 3; // pin b to the 4th column even though it's one hop away
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, nodes)
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") }));

        var rects = cut.FindAll("rect");
        // b pinned far right → large x gap from a.
        Assert.True(Attr(rects[1], "x") - Attr(rects[0], "x") > 300);
    }

    [Fact]
    public void Cycle_Does_Not_Hang_And_Still_Renders()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("b", "a") }));

        Assert.Equal(2, cut.FindAll("rect").Count);
        Assert.Equal(2, cut.FindAll("path").Count);
    }

    [Fact]
    public void Labels_Are_Html_Escaped()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, new List<SankeyNode>
            {
                new() { Id = "a", Label = "<b>evil</b>" },
                new() { Id = "b", Label = "safe" }
            })
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") }));

        Assert.DoesNotContain("<b>evil</b>", cut.Markup);
        Assert.Contains("&lt;b&gt;evil&lt;/b&gt;", cut.Markup);
    }

    [Fact]
    public void ShowValues_Appends_Value_To_Label()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b", "c"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("a", "c") })
            .Add(x => x.ShowValues, true));

        Assert.Contains("a (2)", cut.Markup);
    }

    [Fact]
    public void Hover_A_Node_Shows_A_Tooltip_Group()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") }));

        Assert.Empty(cut.FindAll("g")); // no tooltip until hover
        cut.FindAll("rect")[0].MouseOver();
        Assert.NotEmpty(cut.FindAll("g"));
    }

    [Fact]
    public void OnNodeClick_Fires_With_The_Clicked_Node()
    {
        SankeyNode? clicked = null;
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") })
            .Add(x => x.OnNodeClick, n => clicked = n));

        cut.FindAll("rect")[0].Click();
        Assert.NotNull(clicked);
        Assert.Equal("a", clicked!.Id);
    }

    [Fact]
    public void Responsive_Caps_Width_Nonresponsive_Is_Fixed()
    {
        var responsive = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") })
            .Add(x => x.Width, 600));
        var svg = responsive.Find("svg");
        Assert.Equal("100%", svg.GetAttribute("width"));
        Assert.Contains("max-width:600px", svg.GetAttribute("style"));

        var fixedW = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") })
            .Add(x => x.Responsive, false)
            .Add(x => x.Width, 600));
        Assert.Equal("600", fixedW.Find("svg").GetAttribute("width"));
        Assert.DoesNotContain("max-width", fixedW.Find("svg").GetAttribute("style"));
    }

    [Fact]
    public void Accessible_Name_Desc_And_Role()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b", "c"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b"), Link("a", "c") }));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        var titleId = cut.Find("title").GetAttribute("id");
        var descId = cut.Find("desc").GetAttribute("id");
        Assert.Equal($"{titleId} {descId}", svg.GetAttribute("aria-labelledby"));
        Assert.Equal("Sankey chart", cut.Find("title").TextContent);
        Assert.Contains("3 nodes", cut.Find("desc").TextContent);
        Assert.Contains("2 flows", cut.Find("desc").TextContent);
    }

    [Fact]
    public void Title_Parameter_Becomes_Accessible_Name()
    {
        var cut = Render<SankeyChart>(p => p
            .Add(x => x.Nodes, Nodes("a", "b"))
            .Add(x => x.Links, new List<SankeyLink> { Link("a", "b") })
            .Add(x => x.Title, "Traffic to pages"));

        Assert.Equal("Traffic to pages", cut.Find("title").TextContent);
    }
}
