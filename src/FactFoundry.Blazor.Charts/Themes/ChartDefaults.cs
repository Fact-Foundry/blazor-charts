namespace FactFoundry.Blazor.Charts.Themes;

public static class ChartDefaults
{
    public static readonly IReadOnlyList<string> ColorPalette =
    [
        "#3B82F6", // Blue
        "#10B981", // Emerald
        "#F59E0B", // Amber
        "#EF4444", // Red
        "#8B5CF6", // Violet
        "#06B6D4", // Cyan
        "#F97316", // Orange
        "#EC4899", // Pink
        "#14B8A6", // Teal
        "#6366F1", // Indigo
        "#84CC16", // Lime
        "#A855F7", // Purple
    ];

    public static string GetColor(int index) =>
        ColorPalette[index % ColorPalette.Count];
}
