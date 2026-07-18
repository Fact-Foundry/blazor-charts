using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class CommitGraphTests : BunitContext
{
    // A linear history: C -> B -> A (newest first).
    private static List<CommitNode> Linear() =>
    [
        new() { Id = "c", ParentIds = ["b"], Message = "third" },
        new() { Id = "b", ParentIds = ["a"], Message = "second" },
        new() { Id = "a", ParentIds = [], Message = "first" }
    ];

    // A branch/merge: M merges topic(t) back into main.
    //   m (merge)  parents: p, t
    //   t (topic)  parents: p
    //   p (base)   parents: root
    //   root
    private static List<CommitNode> Merge() =>
    [
        new() { Id = "m", ParentIds = ["p", "t"], Message = "merge topic" },
        new() { Id = "t", ParentIds = ["p"], Message = "topic work" },
        new() { Id = "p", ParentIds = ["root"], Message = "base" },
        new() { Id = "root", ParentIds = [], Message = "root" }
    ];

    [Fact]
    public void Renders_Root_Svg()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Linear()));
        Assert.NotNull(cut.Find("svg.ff-commitgraph"));
    }

    [Fact]
    public void Empty_Commits_Render_Nothing()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, new List<CommitNode>()));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void One_Dot_Per_Commit()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Linear()));
        // Simple (non-merge) commits render exactly one circle each.
        Assert.Equal(3, cut.FindAll("circle").Count);
    }

    [Fact]
    public void Linear_History_Uses_A_Single_Lane()
    {
        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, Linear())
            .Add(x => x.LaneWidth, 16));

        // All dots share the same x → one lane. LaneX(0) = 12 + 0 + 8 = 20.
        var cxs = cut.FindAll("circle").Select(c => c.GetAttribute("cx")).Distinct().ToList();
        Assert.Single(cxs);
    }

    [Fact]
    public void Linear_History_Draws_Connectors_Between_Adjacent_Commits()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Linear()));
        // Two edges: c->b, b->a.
        Assert.Equal(2, cut.FindAll("path").Count);
    }

    [Fact]
    public void Merge_Commit_Renders_Two_Circles()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Merge()));
        // 3 simple commits (1 circle each) + 1 merge commit (2 circles) = 5.
        Assert.Equal(5, cut.FindAll("circle").Count);
    }

    [Fact]
    public void Merge_Uses_At_Least_Two_Lanes()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Merge()));
        var cxs = cut.FindAll("circle").Select(c => c.GetAttribute("cx")).Distinct().Count();
        Assert.True(cxs >= 2, "merge history should occupy more than one lane");
    }

    [Fact]
    public void Merge_Emits_An_Edge_Per_Parent_Link()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Merge()));
        // Parent links: m->p, m->t, t->p, p->root = 4.
        Assert.Equal(4, cut.FindAll("path").Count);
    }

    [Fact]
    public void Renders_Commit_Messages()
    {
        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, Linear()));
        Assert.Contains("third", cut.Markup);
        Assert.Contains("first", cut.Markup);
    }

    [Fact]
    public void ShowMessage_False_Hides_Messages()
    {
        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, Linear())
            .Add(x => x.ShowMessage, false));
        Assert.DoesNotContain("third", cut.Markup);
    }

    [Fact]
    public void Renders_Ref_Badges()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "c", ParentIds = ["b"], Message = "tip", Refs = ["main", "tag: v1.0"] },
            new() { Id = "b", ParentIds = [], Message = "base" }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        Assert.Contains("main", cut.Markup);
        // The "tag:" prefix is stripped for display.
        Assert.Contains(">v1.0<", cut.Markup);
        Assert.DoesNotContain("tag: v1.0", cut.Markup);
    }

    [Fact]
    public void Head_Decoration_Splits_Into_Separate_Badges()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "c", ParentIds = [], Message = "tip", Refs = ["HEAD -> main"] }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));

        // "HEAD -> main" becomes two badges: "HEAD" and "main", with no arrow.
        Assert.Contains(">HEAD<", cut.Markup);
        Assert.Contains(">main<", cut.Markup);
        Assert.DoesNotContain("->", cut.Markup);
        Assert.DoesNotContain("HEAD -&gt; main", cut.Markup);
    }

    [Fact]
    public void Dangling_Parent_Does_Not_Throw()
    {
        // Parent "z" is outside the provided window (truncated history).
        var data = new List<CommitNode>
        {
            new() { Id = "b", ParentIds = ["z"], Message = "only" }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        Assert.Single(cut.FindAll("circle"));
        Assert.Single(cut.FindAll("path")); // one dangling connector
    }

    [Fact]
    public void Escapes_Markup_In_Messages()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "a", ParentIds = [], Message = "<script>alert(1)</script>" }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        Assert.DoesNotContain("<script>", cut.Markup);
        Assert.Contains("&lt;script&gt;", cut.Markup);
    }

    [Fact]
    public void OnCommitClick_Fires_With_The_Clicked_Commit()
    {
        CommitNode? clicked = null;
        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, Linear())
            .Add(x => x.OnCommitClick, c => clicked = c));

        // The last rects are the full-row click targets, in row order.
        var rows = cut.FindAll("rect");
        rows[^2].Click(); // second row → commit "b"
        Assert.NotNull(clicked);
        Assert.Equal("b", clicked!.Id);
    }

    [Fact]
    public void Hover_Shows_Tooltip_With_Author_And_ShortId()
    {
        var data = new List<CommitNode>
        {
            new()
            {
                Id = "abcdef1234567890",
                ParentIds = [],
                Message = "do the thing",
                Author = "Ada Lovelace",
                Date = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero)
            }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));

        // No tooltip until hovered.
        Assert.DoesNotContain("Ada Lovelace", cut.Markup);

        cut.FindAll("rect").Last().MouseOver();

        Assert.Contains("Ada Lovelace", cut.Markup);
        Assert.Contains("abcdef1", cut.Markup);      // 7-char short id
        Assert.Contains("Jan 2, 2026 03:04", cut.Markup);
    }

    [Fact]
    public void Long_Message_Tooltip_Stays_Within_Max_Width()
    {
        // A message far wider than the graph — the reported "box swallows the panel" case.
        var longMsg = string.Join(" ",
            Enumerable.Repeat("Removed the Items measure grouped by order_line_id to avoid duplicates", 6));
        var data = new List<CommitNode>
        {
            new()
            {
                Id = "abc1234def", ParentIds = [], Message = longMsg,
                Author = "Kevin", Date = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        };

        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, data)
            .Add(x => x.TooltipMaxWidth, 360));

        cut.FindAll("rect").Last().MouseOver();

        // The tooltip box is the rect inside the pointer-events:none group.
        var box = cut.Find("g > rect");
        var width = double.Parse(box.GetAttribute("width")!, CultureInfo.InvariantCulture);
        Assert.True(width <= 365, $"tooltip width {width} should stay within TooltipMaxWidth (+padding)");

        // The long message wrapped across several header lines rather than one giant line.
        Assert.True(cut.FindAll("g > text").Count > 3, "long message should wrap onto multiple lines");
    }

    [Fact]
    public void Tooltip_Never_Exceeds_A_Narrow_Chart_Width()
    {
        // TooltipMaxWidth (360) is wider than the chart (280) — the box must clamp to the
        // chart, not overflow/clip. Regression for the "goes out of bounds" case.
        const int width = 280;
        var longMsg = string.Join(" ",
            Enumerable.Repeat("validates every source-backed table against the live schema", 8));
        var data = new List<CommitNode>
        {
            new() { Id = "a", ParentIds = [], Message = longMsg, Author = "Kevin" }
        };

        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, data)
            .Add(x => x.Width, width)
            .Add(x => x.TooltipMaxWidth, 360));

        cut.FindAll("rect").Last().MouseOver();

        var box = cut.Find("g > rect");
        var x = double.Parse(box.GetAttribute("x")!, CultureInfo.InvariantCulture);
        var w = double.Parse(box.GetAttribute("width")!, CultureInfo.InvariantCulture);
        Assert.True(x >= 0 && x + w <= width, $"tooltip [{x}..{x + w}] must fit within chart width {width}");
    }

    [Fact]
    public void TooltipMaxLines_Caps_Wrapped_Header_Lines()
    {
        var longMsg = string.Join(" ", Enumerable.Repeat("word", 200));
        var data = new List<CommitNode> { new() { Id = "a", ParentIds = [], Message = longMsg } };

        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, data)
            .Add(x => x.TooltipMaxLines, 2));

        cut.FindAll("rect").Last().MouseOver();

        // 2 header lines + the short-id meta line = 3 text runs in the tooltip group.
        Assert.Equal(3, cut.FindAll("g > text").Count);
    }

    [Fact]
    public void Tooltip_Collapses_Newlines_In_Message()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "a", ParentIds = [], Message = "subject line\n\nbody paragraph here", Author = "Kevin" }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        cut.FindAll("rect").Last().MouseOver();

        // No raw newline survives into the rendered SVG text runs.
        Assert.DoesNotContain("subject line\n", cut.Markup);
        Assert.Contains("subject line", cut.Markup);
        Assert.Contains("body paragraph", cut.Markup);
    }

    [Fact]
    public void Tooltip_Shows_Colored_File_Change_Stats()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "a", ParentIds = [], Message = "m", FilesChanged = 2, Insertions = 4, Deletions = 3 }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        cut.FindAll("rect").Last().MouseOver();

        Assert.Contains("2 files changed", cut.Markup);
        Assert.Contains("+4", cut.Markup);
        Assert.Contains("-3", cut.Markup);
        Assert.Contains("#22c55e", cut.Markup); // insertions green
        Assert.Contains("#ef4444", cut.Markup); // deletions red
    }

    [Fact]
    public void Commit_Id_Is_The_Last_Tooltip_Line()
    {
        var data = new List<CommitNode>
        {
            new()
            {
                Id = "abcdef123", ParentIds = [], Message = "m", Author = "Kevin",
                Date = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero),
                FilesChanged = 2, Insertions = 4, Deletions = 3
            }
        };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        cut.FindAll("rect").Last().MouseOver();

        var texts = cut.FindAll("g > text");
        double Y(IElement t) => double.Parse(t.GetAttribute("y")!, CultureInfo.InvariantCulture);

        var idLine = texts.Single(t => t.TextContent.Contains('·'));
        var statsLine = texts.Single(t => t.TextContent.Contains("files changed"));

        // The commit id/date line sits below the stats row, and is the bottom-most line.
        Assert.True(Y(idLine) > Y(statsLine));
        Assert.Equal(texts.Max(Y), Y(idLine));
    }

    [Fact]
    public void Stats_Row_Absent_When_No_Stat_Fields_Set()
    {
        var data = new List<CommitNode> { new() { Id = "a", ParentIds = [], Message = "m" } };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        cut.FindAll("rect").Last().MouseOver();

        Assert.DoesNotContain("files changed", cut.Markup);
        Assert.DoesNotContain("#22c55e", cut.Markup);
    }

    [Fact]
    public void Stats_Row_Uses_Singular_For_One_File()
    {
        var data = new List<CommitNode> { new() { Id = "a", ParentIds = [], Message = "m", FilesChanged = 1 } };

        var cut = Render<CommitGraph>(p => p.Add(x => x.Commits, data));
        cut.FindAll("rect").Last().MouseOver();

        Assert.Contains("1 file changed", cut.Markup);
        Assert.DoesNotContain("1 files changed", cut.Markup);
    }

    [Fact]
    public void ShowTooltip_False_Suppresses_Tooltip_On_Hover()
    {
        var data = new List<CommitNode>
        {
            new() { Id = "a", ParentIds = [], Message = "m", Author = "Grace Hopper" }
        };

        var cut = Render<CommitGraph>(p => p
            .Add(x => x.Commits, data)
            .Add(x => x.ShowTooltip, false));

        cut.FindAll("rect").Last().MouseOver();
        Assert.DoesNotContain("Grace Hopper", cut.Markup);
    }
}
