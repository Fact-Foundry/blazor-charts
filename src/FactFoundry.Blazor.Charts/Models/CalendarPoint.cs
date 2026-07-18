namespace FactFoundry.Blazor.Charts.Models;

/// <summary>
/// One day's value for a <c>CalendarHeatmap</c>. Multiple points on the same
/// <see cref="Date"/> are summed, so a raw event list can be passed as-is.
/// </summary>
public class CalendarPoint
{
    /// <summary>The calendar day this value falls on.</summary>
    public DateOnly Date { get; set; }

    /// <summary>The value for the day; drives the cell's color intensity.</summary>
    public decimal Value { get; set; }
}
