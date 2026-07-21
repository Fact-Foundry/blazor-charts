namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// One node in a <c>SankeyChart</c> — a box in a column that flows connect to. The chart
/// knows nothing about the domain; any set of nodes joined by weighted <see cref="SankeyLink"/>s
/// renders as a left-to-right flow. Columns (layers) are computed from the link structure
/// unless pinned with <see cref="Layer"/>.
/// </summary>
public class SankeyNode
{
    /// <summary>Unique id, referenced by <see cref="SankeyLink.Source"/>/<see cref="SankeyLink.Target"/>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label beside the node. Falls back to <see cref="Id"/> when unset.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// Node (and default outgoing-ribbon) color. Falls back to a themed palette color
    /// assigned by the node's position when unset.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional explicit column index (0 = leftmost). When unset the layer is derived from
    /// the flow graph (longest path from a source); terminal nodes are pushed to the last column.
    /// </summary>
    public int? Layer { get; set; }
}
