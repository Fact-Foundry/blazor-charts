using System.Globalization;
using Microsoft.AspNetCore.Components;
using FactFoundry.Blazor.Charts.Models;
using FactFoundry.Blazor.Charts.Themes;

namespace FactFoundry.Blazor.Charts.Components;

/// <summary>
/// A Sankey diagram — nodes in left-to-right columns joined by ribbons whose thickness is
/// proportional to a flow value. Rendered as pure Razor-to-SVG in the same idiom as the
/// other charts and themed through <c>ChartThemeProvider</c>. Columns are derived from the
/// link graph (longest path from a source, terminal nodes pushed right) unless a node pins
/// its own <see cref="SankeyNode.Layer"/>, so anything from a two-column membership diagram
/// to a multi-stage funnel renders from the same <see cref="Nodes"/> + <see cref="Links"/>.
/// </summary>
public partial class SankeyChart : ComponentBase
{
    /// <summary>The nodes to place. Order is the initial within-column order before relaxation.</summary>
    [Parameter] public List<SankeyNode> Nodes { get; set; } = [];

    /// <summary>The weighted flows between nodes. Links to unknown node ids are ignored.</summary>
    [Parameter] public List<SankeyLink> Links { get; set; } = [];

    /// <summary>Overall width in SVG units; when <see cref="Responsive"/> is on it also caps the on-screen width. Defaults to 720.</summary>
    [Parameter] public int Width { get; set; } = 720;

    /// <summary>Overall height in SVG units. Defaults to 480.</summary>
    [Parameter] public int Height { get; set; } = 480;

    /// <summary>Width of each node bar, in SVG units. Defaults to 16.</summary>
    [Parameter] public double NodeWidth { get; set; } = 16;

    /// <summary>Vertical gap between nodes sharing a column, in SVG units. Defaults to 14.</summary>
    [Parameter] public double NodePadding { get; set; } = 14;

    /// <summary>Relaxation passes used to reduce ribbon crossings. Defaults to 6.</summary>
    [Parameter] public int Iterations { get; set; } = 6;

    /// <summary>Fill opacity of the ribbons. Defaults to 0.45.</summary>
    [Parameter] public double LinkOpacity { get; set; } = 0.45;

    /// <summary>Base font size for node labels, in SVG units. Defaults to 12.</summary>
    [Parameter] public double FontSize { get; set; } = 12;

    /// <summary>Show node labels. Defaults to true.</summary>
    [Parameter] public bool ShowNodeLabels { get; set; } = true;

    /// <summary>Append each node's total value to its label. Defaults to false.</summary>
    [Parameter] public bool ShowValues { get; set; }

    /// <summary>.NET numeric format for values in labels and the tooltip (e.g. "N0"). Defaults to a compact form.</summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>Show a themed hover tooltip on nodes and ribbons. Defaults to true.</summary>
    [Parameter] public bool ShowTooltip { get; set; } = true;

    /// <summary>Fill the container width (SVG <c>width="100%"</c>), capped at <see cref="Width"/> px. Defaults to true.</summary>
    [Parameter] public bool Responsive { get; set; } = true;

    /// <summary>Raised when a node is clicked.</summary>
    [Parameter] public EventCallback<SankeyNode> OnNodeClick { get; set; }

    /// <summary>Optional accessible name; falls back to <see cref="Title"/> then "Sankey chart".</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Accessible description read by screen readers, emitted as the SVG <c>&lt;desc&gt;</c>. Auto-generated when unset.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Explicit theme override. Falls back to the cascading theme, then <see cref="ChartTheme.Light"/>.</summary>
    [Parameter] public ChartTheme? Theme { get; set; }

    [CascadingParameter] private ChartTheme? CascadingTheme { get; set; }

    private ChartTheme ResolvedTheme => Theme ?? CascadingTheme ?? ChartTheme.Light;

    private readonly string _a11yId = "ffc-" + Guid.NewGuid().ToString("N")[..8];
    private string TitleId => _a11yId + "t";
    private string DescId => _a11yId + "d";
    private string AccessibleName => string.IsNullOrWhiteSpace(Title) ? "Sankey chart" : Title!;
    private string AccessibleDescription => Description ?? BuildAccessibleDescription();

    private string BuildAccessibleDescription()
    {
        if (_layout.Nodes.Count == 0) return "Sankey chart with no data.";
        var busiest = _layout.Nodes.OrderByDescending(n => n.Value).First();
        return $"Sankey chart of {_layout.Nodes.Count} nodes and {_layout.Links.Count} flows across " +
               $"{_layout.LayerCount} column{(_layout.LayerCount == 1 ? "" : "s")}. " +
               $"Largest node {busiest.Label} at {FormatValue(busiest.Value)}.";
    }

    // Hover state: a node id, or a link index. Only one is set at a time.
    private string? _hoverNode;
    private int? _hoverLink;

    // ---- Layout model ---------------------------------------------------------------

    private sealed record PNode(
        string Id, string Label, int Layer, double X, double Y, double H, string Color, double Value);

    private sealed record PLink(
        int Index, string Source, string Target, double Value, string Color,
        string Path, double LabelX, double LabelY);

    private sealed record SankeyLayout(
        List<PNode> Nodes, List<PLink> Links, int LayerCount,
        Dictionary<string, List<int>> NodeLinks, Dictionary<string, SankeyNode> Source);

    private SankeyLayout _layout = new([], [], 0, [], []);

    protected override void OnParametersSet() => _layout = BuildLayout();

    // ---- Geometry constants ---------------------------------------------------------

    private const double OuterPad = 10;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double d) => d.ToString("F1", Inv);
    private double LabelFont => FontSize;
    private double TextWidth(string s) => s.Length * LabelFont * 0.58;

    private string FormatValue(double v) =>
        ValueFormat is not null
            ? v.ToString(ValueFormat, Inv)
            : v.ToString("0.##", Inv);

    // Mutable working state during layout.
    private sealed class WNode
    {
        public string Id = "";
        public string Label = "";
        public int Layer;
        public double Value;
        public double Y;
        public double H;
        public string Color = "";
        public double Center => Y + H / 2;
    }

    private sealed class WLink
    {
        public int Index;
        public WNode S = null!;
        public WNode T = null!;
        public double Value;
        public double W;
        public double Sy;
        public double Ty;
        public string Color = "";
    }

    private SankeyLayout BuildLayout()
    {
        var nodes = Nodes;
        if (nodes.Count == 0) return new([], [], 0, [], []);

        var byId = new Dictionary<string, WNode>();
        var srcById = new Dictionary<string, SankeyNode>();
        var order = new List<WNode>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (string.IsNullOrEmpty(n.Id) || byId.ContainsKey(n.Id)) continue;
            var w = new WNode
            {
                Id = n.Id,
                Label = string.IsNullOrEmpty(n.Label) ? n.Id : n.Label!,
                Color = n.Color ?? ResolvedTheme.GetColor(order.Count),
                Layer = n.Layer ?? -1,
            };
            byId[n.Id] = w;
            srcById[n.Id] = n;
            order.Add(w);
        }

        // Valid links only (both endpoints known, non-self, positive value).
        var wlinks = new List<WLink>();
        for (var i = 0; i < Links.Count; i++)
        {
            var l = Links[i];
            if (l.Value <= 0) continue;
            if (!byId.TryGetValue(l.Source, out var s) || !byId.TryGetValue(l.Target, out var t)) continue;
            if (ReferenceEquals(s, t)) continue;
            wlinks.Add(new WLink { Index = i, S = s, T = t, Value = l.Value, Color = l.Color ?? s.Color });
        }

        AssignLayers(order, wlinks);
        var layerCount = order.Max(n => n.Layer) + 1;

        // Node value = max(inflow, outflow).
        foreach (var n in order)
        {
            var inSum = wlinks.Where(l => l.T == n).Sum(l => l.Value);
            var outSum = wlinks.Where(l => l.S == n).Sum(l => l.Value);
            n.Value = Math.Max(inSum, outSum);
        }

        // Reserve horizontal room for the first/last column labels.
        double leftLabelW = 0, rightLabelW = 0;
        if (ShowNodeLabels)
        {
            leftLabelW = order.Where(n => n.Layer == 0).Select(n => TextWidth(NodeText(n))).DefaultIfEmpty(0).Max();
            rightLabelW = order.Where(n => n.Layer == layerCount - 1).Select(n => TextWidth(NodeText(n))).DefaultIfEmpty(0).Max();
        }
        var leftPad = OuterPad + (leftLabelW > 0 ? leftLabelW + 8 : 0);
        var rightPad = OuterPad + (rightLabelW > 0 ? rightLabelW + 8 : 0);
        var topPad = OuterPad + (layerCount > 2 && ShowNodeLabels ? LabelFont + 4 : 0);
        var botPad = OuterPad;
        var availH = Math.Max(1, Height - topPad - botPad);

        // Vertical scale: pick the ky that lets the tightest column fit.
        double ky = double.PositiveInfinity;
        for (var layer = 0; layer < layerCount; layer++)
        {
            var col = order.Where(n => n.Layer == layer).ToList();
            var colValue = col.Sum(n => n.Value);
            if (colValue <= 0) continue;
            var gaps = Math.Max(0, col.Count - 1) * NodePadding;
            ky = Math.Min(ky, (availH - gaps) / colValue);
        }
        if (double.IsInfinity(ky) || ky <= 0) ky = 1;

        foreach (var n in order) n.H = n.Value * ky;
        foreach (var l in wlinks) l.W = l.Value * ky;

        // X position per column.
        double X(int layer) => layerCount <= 1
            ? leftPad
            : leftPad + layer * (Width - leftPad - rightPad - NodeWidth) / (layerCount - 1);

        // Initial vertical stack, centered per column.
        var byLayer = new List<WNode>[layerCount];
        for (var layer = 0; layer < layerCount; layer++)
        {
            var col = order.Where(n => n.Layer == layer).ToList();
            byLayer[layer] = col;
            var totalH = col.Sum(n => n.H) + Math.Max(0, col.Count - 1) * NodePadding;
            var y = topPad + Math.Max(0, (availH - totalH) / 2);
            foreach (var n in col) { n.Y = y; y += n.H + NodePadding; }
        }

        // Relax: barycenter toward neighbors, then resolve overlaps. Two sweeps per pass.
        var alpha = 0.97;
        for (var it = 0; it < Iterations; it++)
        {
            for (var layer = layerCount - 2; layer >= 0; layer--)
                RelaxTowards(byLayer[layer], n => wlinks.Where(l => l.S == n).Select(l => (l.T, l.Value)), alpha);
            foreach (var col in byLayer) ResolveCollisions(col, topPad, availH);

            for (var layer = 1; layer < layerCount; layer++)
                RelaxTowards(byLayer[layer], n => wlinks.Where(l => l.T == n).Select(l => (l.S, l.Value)), alpha);
            foreach (var col in byLayer) ResolveCollisions(col, topPad, availH);

            alpha *= 0.97;
        }

        // Assign ribbon offsets along each node edge, ordered to reduce crossings.
        foreach (var n in order)
        {
            var c = n.Y;
            foreach (var l in wlinks.Where(l => l.S == n).OrderBy(l => l.T.Center)) { l.Sy = c; c += l.W; }
            c = n.Y;
            foreach (var l in wlinks.Where(l => l.T == n).OrderBy(l => l.S.Center)) { l.Ty = c; c += l.W; }
        }

        // Emit immutable placed records.
        var placedNodes = order
            .Where(n => n.H > 0.01)
            .Select(n => new PNode(n.Id, NodeText(n), n.Layer, X(n.Layer), n.Y, n.H, n.Color, n.Value))
            .ToList();

        var nodeLinks = new Dictionary<string, List<int>>();
        var placedLinks = new List<PLink>();
        foreach (var l in wlinks)
        {
            var x0 = X(l.S.Layer) + NodeWidth;
            var x1 = X(l.T.Layer);
            var cx = (x0 + x1) / 2;
            var path =
                $"M{F(x0)} {F(l.Sy)} " +
                $"C{F(cx)} {F(l.Sy)} {F(cx)} {F(l.Ty)} {F(x1)} {F(l.Ty)} " +
                $"L{F(x1)} {F(l.Ty + l.W)} " +
                $"C{F(cx)} {F(l.Ty + l.W)} {F(cx)} {F(l.Sy + l.W)} {F(x0)} {F(l.Sy + l.W)} Z";
            placedLinks.Add(new PLink(l.Index, l.S.Id, l.T.Id, l.Value, l.Color, path, cx, (l.Sy + l.Ty + l.W) / 2));
            (nodeLinks.TryGetValue(l.S.Id, out var a) ? a : nodeLinks[l.S.Id] = []).Add(placedLinks.Count - 1);
            (nodeLinks.TryGetValue(l.T.Id, out var b) ? b : nodeLinks[l.T.Id] = []).Add(placedLinks.Count - 1);
        }

        return new SankeyLayout(placedNodes, placedLinks, Math.Max(layerCount, 1), nodeLinks, srcById);
    }

    private string NodeText(WNode n) =>
        ShowValues ? $"{n.Label} ({FormatValue(n.Value)})" : n.Label;

    // Longest-path layering with a cycle guard; unpinned terminal nodes pushed to the last column.
    private void AssignLayers(List<WNode> nodes, List<WLink> links)
    {
        var incoming = nodes.ToDictionary(n => n, n => links.Where(l => l.T == n).ToList());
        var outgoing = nodes.ToDictionary(n => n, n => links.Where(l => l.S == n).ToList());
        var depth = new Dictionary<WNode, int>();
        var stack = new HashSet<WNode>();

        int Depth(WNode n)
        {
            if (n.Layer >= 0) return n.Layer;              // pinned
            if (depth.TryGetValue(n, out var d)) return d;
            if (!stack.Add(n)) return 0;                    // cycle: break
            var best = 0;
            foreach (var l in incoming[n])
                best = Math.Max(best, Depth(l.S) + 1);
            stack.Remove(n);
            return depth[n] = best;
        }

        foreach (var n in nodes)
            n.Layer = n.Layer >= 0 ? n.Layer : Depth(n);

        var maxLayer = nodes.Count == 0 ? 0 : nodes.Max(n => n.Layer);
        foreach (var n in nodes)
            if (n.Layer < maxLayer && outgoing[n].Count == 0)
                n.Layer = maxLayer;                         // right-align sinks
    }

    private void RelaxTowards(List<WNode> col, Func<WNode, IEnumerable<(WNode Node, double Value)>> neighbors, double alpha)
    {
        foreach (var n in col)
        {
            double sum = 0, wsum = 0;
            foreach (var (node, value) in neighbors(n)) { sum += node.Center * value; wsum += value; }
            if (wsum <= 0) continue;
            var target = sum / wsum;
            n.Y += (target - n.Center) * alpha;
        }
    }

    private void ResolveCollisions(List<WNode> col, double top, double availH)
    {
        col.Sort((a, b) => a.Y.CompareTo(b.Y));
        var y = top;
        foreach (var n in col) { if (n.Y < y) n.Y = y; y = n.Y + n.H + NodePadding; }

        var bottom = top + availH;
        y = bottom;
        for (var i = col.Count - 1; i >= 0; i--)
        {
            var n = col[i];
            if (n.Y + n.H > y) n.Y = y - n.H;
            y = n.Y - NodePadding;
        }
    }

    // ---- Interaction ----------------------------------------------------------------

    private void OnNodeOver(string id) { _hoverNode = id; _hoverLink = null; }
    private void OnLinkOver(int idx) { _hoverLink = idx; _hoverNode = null; }
    private void OnOut() { _hoverNode = null; _hoverLink = null; }

    private async Task NodeClick(string id)
    {
        if (OnNodeClick.HasDelegate && _layout.Source.TryGetValue(id, out var n))
            await OnNodeClick.InvokeAsync(n);
    }

    // Dim everything not incident to the hovered node/link.
    private bool IsActive => _hoverNode is not null || _hoverLink is not null;

    private bool LinkLit(int listIndex)
    {
        if (_hoverLink is int hl) return listIndex == hl;
        if (_hoverNode is string hn) return _layout.NodeLinks.TryGetValue(hn, out var ids) && ids.Contains(listIndex);
        return true;
    }

    private bool NodeLit(string id)
    {
        if (_hoverNode is string hn) return id == hn || Adjacent(hn, id);
        if (_hoverLink is int hl)
        {
            var l = _layout.Links[hl];
            return id == l.Source || id == l.Target;
        }
        return true;
    }

    private bool Adjacent(string a, string b)
    {
        if (!_layout.NodeLinks.TryGetValue(a, out var ids)) return false;
        foreach (var i in ids)
        {
            var l = _layout.Links[i];
            if (l.Source == b || l.Target == b) return true;
        }
        return false;
    }

    private static string Escape(string? s) =>
        string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
