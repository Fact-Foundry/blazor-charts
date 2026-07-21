namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// A weighted flow between two <see cref="SankeyNode"/>s in a <c>SankeyChart</c>. The
/// ribbon's thickness is proportional to <see cref="Value"/> — leave it at the default
/// <c>1</c> for a uniform-thickness membership diagram, or set it to a real magnitude
/// (volume, count, weight) for a true flow.
/// </summary>
public class SankeyLink
{
    /// <summary>Id of the source node (the ribbon leaves its right edge).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Id of the target node (the ribbon meets its left edge).</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Flow magnitude — sets the ribbon thickness. Defaults to <c>1</c> (uniform).</summary>
    public double Value { get; set; } = 1;

    /// <summary>Ribbon color. Falls back to the source node's color when unset.</summary>
    public string? Color { get; set; }
}
