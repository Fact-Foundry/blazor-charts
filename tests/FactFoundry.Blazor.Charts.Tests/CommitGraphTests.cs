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
        Assert.Contains("2026-01-02 03:04", cut.Markup);
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
