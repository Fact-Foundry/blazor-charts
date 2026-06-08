namespace FactFoundry.Blazor.Charts.Themes;

public class ChartTheme
{
    public string TextColor { get; init; } = "currentColor";
    public string GridColor { get; init; } = "currentColor";
    public double GridOpacity { get; init; } = 0.1;
    public string CrosshairColor { get; init; } = "currentColor";
    public double CrosshairOpacity { get; init; } = 0.25;
    public string TooltipBackground { get; init; } = "#000000";
    public double TooltipOpacity { get; init; } = 0.88;
    public string TooltipTextColor { get; init; } = "#ffffff";
    public double LabelOpacity { get; init; } = 0.7;
    public IReadOnlyList<string>? Palette { get; init; }

    public string GetColor(int index)
    {
        var palette = Palette ?? ChartDefaults.ColorPalette;
        return palette[index % palette.Count];
    }

    public static ChartTheme Light { get; } = new();

    public static ChartTheme Dark { get; } = new()
    {
        TextColor = "#e2e8f0",
        GridColor = "#475569",
        GridOpacity = 0.3,
        CrosshairColor = "#94a3b8",
        CrosshairOpacity = 0.4,
        TooltipBackground = "#1e293b",
        TooltipOpacity = 0.95,
        TooltipTextColor = "#f1f5f9",
        LabelOpacity = 0.8,
    };
}
