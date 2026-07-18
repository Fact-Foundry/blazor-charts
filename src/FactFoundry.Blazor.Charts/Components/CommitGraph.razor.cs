using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A git-style commit graph — a vertical list of commits with a lane strip on the left
/// where branches are color-coded columns and merges/branch points are drawn as curves
/// between lanes. Rendered as pure Razor-to-SVG in the same idiom as the other charts,
/// themed through <c>ChartThemeProvider</c>. The layout is a single lane-assignment pass
/// over the parent links; the component does no ordering, so hand it commits already in
/// display order (newest first), exactly as a LibGit2Sharp topological/time-sorted walk
/// yields them.
/// </summary>
public partial class CommitGraph : ComponentBase
{
    /// <summary>The commits to draw, in display order — newest (row 0) at the top.</summary>
    [Parameter] public List<CommitNode> Commits { get; set; } = [];

    /// <summary>Vertical distance between commit rows, in SVG units. Defaults to 28.</summary>
    [Parameter] public int RowHeight { get; set; } = 28;

    /// <summary>Horizontal distance between lanes, in SVG units. Defaults to 16.</summary>
    [Parameter] public int LaneWidth { get; set; } = 16;

    /// <summary>Radius of the commit dots. Defaults to 4.5.</summary>
    [Parameter] public double DotRadius { get; set; } = 4.5;

    /// <summary>Overall component width in SVG units; the message column flexes to fill it. Defaults to 720.</summary>
    [Parameter] public int Width { get; set; } = 720;

    /// <summary>Show each commit's subject line beside the lane strip. Defaults to true.</summary>
    [Parameter] public bool ShowMessage { get; set; } = true;

    /// <summary>Show branch/tag ref badges on their commits. Defaults to true.</summary>
    [Parameter] public bool ShowRefs { get; set; } = true;

    /// <summary>Show a themed hover tooltip with author, short id, and date. Defaults to true.</summary>
    [Parameter] public bool ShowTooltip { get; set; } = true;

    /// <summary>Number of leading id characters to show as the short id. Defaults to 7.</summary>
    [Parameter] public int ShortIdLength { get; set; } = 7;

    /// <summary>Format string for the tooltip date. Defaults to <c>yyyy-MM-dd HH:mm</c>.</summary>
    [Parameter] public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm";

    /// <summary>Scale to the container width (SVG <c>width="100%"</c>). Defaults to true.</summary>
    [Parameter] public bool Responsive { get; set; } = true;

    /// <summary>Raised when a commit row is clicked.</summary>
    [Parameter] public EventCallback<CommitNode> OnCommitClick { get; set; }

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private int? _hoveredRow;

    // ---- Layout model ---------------------------------------------------------------

    /// <summary>A commit resolved to a row and lane, ready to draw.</summary>
    private sealed record Placed(int Row, int Lane, CommitNode Commit);

    /// <summary>A connector from a child commit down to one parent.</summary>
    private sealed record Edge(int FromRow, int FromLane, int ToRow, int ToLane, int ColorLane, bool Dangling);

    private sealed record Layout(List<Placed> Nodes, List<Edge> Edges, int LaneCount);

    private Layout _layout = new([], [], 1);

    // The lane assignment depends only on the commit DAG, so recompute it when parameters
    // change — not on every render. Hover/click re-renders (which flip _hoveredRow via
    // StateHasChanged, without setting parameters) then reuse the cached layout instead of
    // re-running the O(n·lanes) pass over the whole history.
    protected override void OnParametersSet() => _layout = BuildLayout();

    private Layout BuildLayout()
    {
        var commits = Commits;
        var n = commits.Count;
        var indexById = new Dictionary<string, int>(n);
        for (var i = 0; i < n; i++)
            indexById[commits[i].Id] = i;

        // Active lanes; each holds the id of the commit that lane is next expecting, or null if free.
        var lanes = new List<string?>();
        var commitLane = new int[n];
        var placed = new List<Placed>(n);
        // Edges captured with the lane reserved for the parent at capture time; the vertical
        // run is re-homed to the parent's actual lane once we know it (post-pass).
        var pending = new List<(int FromRow, string ParentId, int ReservedLane)>();
        var laneCount = 0;

        int FindLaneExpecting(string id)
        {
            for (var l = 0; l < lanes.Count; l++)
                if (lanes[l] == id) return l;
            return -1;
        }

        int AllocLane(string? expect)
        {
            for (var l = 0; l < lanes.Count; l++)
            {
                if (lanes[l] is null)
                {
                    lanes[l] = expect;
                    return l;
                }
            }
            lanes.Add(expect);
            return lanes.Count - 1;
        }

        for (var r = 0; r < n; r++)
        {
            var c = commits[r];

            // The commit takes the leftmost lane already expecting it, or a fresh lane if it is a tip.
            var lane = FindLaneExpecting(c.Id);
            if (lane < 0)
                lane = AllocLane(null);
            commitLane[r] = lane;
            placed.Add(new Placed(r, lane, c));

            // Other lanes that also expected this commit are branches converging here; release them.
            for (var l = 0; l < lanes.Count; l++)
                if (l != lane && lanes[l] == c.Id)
                    lanes[l] = null;

            // Route to parents. First parent continues straight down this lane; the rest fan out.
            if (c.ParentIds.Count == 0)
            {
                lanes[lane] = null; // root — the lane ends here
            }
            else
            {
                for (var p = 0; p < c.ParentIds.Count; p++)
                {
                    var parentId = c.ParentIds[p];
                    int parentLane;
                    if (p == 0)
                    {
                        lanes[lane] = parentId;
                        parentLane = lane;
                    }
                    else
                    {
                        var existing = FindLaneExpecting(parentId);
                        parentLane = existing >= 0 ? existing : AllocLane(parentId);
                    }
                    pending.Add((r, parentId, parentLane));
                }
            }

            laneCount = Math.Max(laneCount, lanes.Count);
        }

        // Resolve each edge's destination: to the parent's real lane if the parent is in view,
        // otherwise dangle straight down off the bottom in the lane we reserved for it.
        var edges = new List<Edge>(pending.Count);
        foreach (var (fromRow, parentId, reservedLane) in pending)
        {
            var fromLane = commitLane[fromRow];
            if (indexById.TryGetValue(parentId, out var toRow))
            {
                var toLane = commitLane[toRow];
                edges.Add(new Edge(fromRow, fromLane, toRow, toLane, toLane, Dangling: false));
            }
            else
            {
                edges.Add(new Edge(fromRow, fromLane, n, reservedLane, reservedLane, Dangling: true));
            }
        }

        return new Layout(placed, edges, Math.Max(laneCount, 1));
    }

    // ---- Geometry -------------------------------------------------------------------

    private const int PadTop = 6;
    private const int StripPadLeft = 12;
    private const int MessageGap = 14;

    private double LaneX(int lane) => StripPadLeft + lane * LaneWidth + LaneWidth / 2.0;

    private double RowY(int row) => PadTop + row * RowHeight + RowHeight / 2.0;

    private double StripWidth(int laneCount) => StripPadLeft + laneCount * LaneWidth;

    private double MessageX(int laneCount) => StripWidth(laneCount) + MessageGap;

    private int TotalHeight => PadTop * 2 + Math.Max(Commits.Count, 0) * RowHeight;

    private string LaneColor(int lane) => ResolvedTheme.GetColor(lane);

    private static string F(double d) => d.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>
    /// Path from a child dot down to a parent dot: straight when both share a lane,
    /// otherwise a single-row bend into the parent's lane followed by a vertical run.
    /// </summary>
    private string EdgePath(Edge e)
    {
        var x1 = LaneX(e.FromLane);
        var y1 = RowY(e.FromRow);
        var x2 = LaneX(e.ToLane);
        var y2 = e.Dangling ? TotalHeight : RowY(e.ToRow);

        if (Math.Abs(x1 - x2) < 0.01)
            return $"M{F(x1)} {F(y1)} L{F(x2)} {F(y2)}";

        // Bend across one row height, then drop straight down the destination lane.
        var bendY = Math.Min(y1 + RowHeight, y2);
        return $"M{F(x1)} {F(y1)} " +
               $"C{F(x1)} {F(y1 + RowHeight * 0.55)} {F(x2)} {F(bendY - RowHeight * 0.55)} {F(x2)} {F(bendY)} " +
               $"L{F(x2)} {F(y2)}";
    }

    // ---- Refs & tooltip -------------------------------------------------------------

    private readonly record struct RefBadge(string Text, bool IsTag);

    private static RefBadge ParseRef(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            return new RefBadge(trimmed[4..].Trim(), IsTag: true);
        return new RefBadge(trimmed, IsTag: false);
    }

    private string ShortId(string id) =>
        id.Length <= ShortIdLength ? id : id[..ShortIdLength];

    private string FormatDate(DateTimeOffset date) =>
        date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private async Task HandleClick(CommitNode commit)
    {
        if (OnCommitClick.HasDelegate)
            await OnCommitClick.InvokeAsync(commit);
    }

    private void OnRowOver(int row) => _hoveredRow = row;
    private void OnRowOut() => _hoveredRow = null;

    private static string Escape(string? s) =>
        string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>Rough width of an SVG string at the given font size, for sizing the tooltip box.</summary>
    private static double TextWidth(string s, double fontSize) => s.Length * fontSize * 0.58;

    private static string Truncate(string s, int maxChars)
    {
        if (maxChars <= 0) return string.Empty;
        return s.Length <= maxChars ? s : s[..(maxChars - 1)] + "…";
    }

    private static int MaxChars(double available, double fontSize) =>
        (int)(available / (fontSize * 0.58));
}
