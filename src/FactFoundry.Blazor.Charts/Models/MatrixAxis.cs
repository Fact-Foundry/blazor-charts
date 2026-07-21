namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// A row in a <c>MatrixChart</c> (a horizontal band). Rows and columns are separate types
/// so a consumer can't accidentally swap them, but they carry the same shape.
/// </summary>
public class MatrixRow
{
    /// <summary>Unique id, referenced by <see cref="MatrixCell.Row"/>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label shown at the start of the row. Falls back to <see cref="Id"/>.</summary>
    public string? Label { get; set; }

    /// <summary>Row color, used to fill this row's cells unless overridden. Falls back to a themed palette color.</summary>
    public string? Color { get; set; }
}

/// <summary>A column in a <c>MatrixChart</c> (a vertical band).</summary>
public class MatrixColumn
{
    /// <summary>Unique id, referenced by <see cref="MatrixCell.Column"/>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label shown above the column. Falls back to <see cref="Id"/>.</summary>
    public string? Label { get; set; }

    /// <summary>Column color, used when <c>ColorByColumn</c> is set. Falls back to a themed palette color.</summary>
    public string? Color { get; set; }
}
