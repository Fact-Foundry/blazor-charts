namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// One filled cell in a <c>MatrixChart</c> — the intersection of a row and a column. Only
/// the intersections you provide are drawn; every other cell reads as empty. <see cref="Value"/>
/// drives the fill intensity (leave it at <c>1</c> for a plain membership grid); a same
/// (row, column) pair given twice is summed.
/// </summary>
public class MatrixCell
{
    /// <summary>Id of the row this cell sits in (matches a <see cref="MatrixRow.Id"/>).</summary>
    public string Row { get; set; } = string.Empty;

    /// <summary>Id of the column this cell sits in (matches a <see cref="MatrixColumn.Id"/>).</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Cell magnitude — sets fill intensity relative to the busiest cell. Defaults to <c>1</c>.</summary>
    public double Value { get; set; } = 1;

    /// <summary>Fill color. Falls back to the row's color (or the column's, per <c>ColorByColumn</c>).</summary>
    public string? Color { get; set; }
}
