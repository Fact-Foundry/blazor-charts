# FactFoundry.Blazor.Charts

A zero-dependency, pure .NET charting library for Blazor applications. Charts are rendered as SVG via Razor components — no JavaScript, no interop, no external dependencies.

## Features

- **Line Chart** — single/multi-series, smooth curves, area fill, crosshair tooltips
- **Bar Chart** — vertical/horizontal, grouped/stacked, crosshair tooltips
- **Donut Chart** — configurable inner radius, "Top N + Other" grouping, center label, leader line labels
- **Pie Chart** — proportional segments with leader line labels
- **World Map Chart** — choropleth heatmap with 174 countries, multi-stop color scales
- **Bar List** — ranked "top N" breakdown with a bar filling behind each row, share %, status dots
- **Sparkline** — bare inline trend (no axes) for KPI tiles; legible down to ~80×30
- **Commit Graph** — git-style branch/commit lane view; color-coded lanes, merge curves, ref badges, hover tooltips
- **Calendar Heatmap** — GitHub-contributions-style day grid; intensity buckets, month/weekday labels, legend, hover tooltips
- **Theming** — built-in light/dark presets, cascading theme provider, fully customizable

All charts include legends, hover tooltips, and accessible SVG output — `role="img"` with a `<title>` name and a data-aware `<desc>` description (override via the `Description` parameter). Works in both Blazor Server and Blazor WebAssembly.

## Installation

```
dotnet add package FactFoundry.Blazor.Charts
```

## Quick Start

Add the using directives to your `_Imports.razor`:

```razor
@using FactFoundry.Blazor.Charts.Components
@using FactFoundry.Blazor.Charts.Models
```

### Line Chart

```razor
<LineChart Title="Sessions Over Time"
           Series="@series"
           XAxisLabels="@labels"
           SmoothLines="true"
           CrosshairTooltip="true"
           Width="700" Height="350" />

@code {
    private List<string> labels = ["Mon", "Tue", "Wed", "Thu", "Fri"];

    private List<ChartSeries> series =
    [
        new() { Label = "Chrome", Color = "#3B82F6", Values = [120, 135, 142, 128, 155] },
        new() { Label = "Firefox", Color = "#F59E0B", Values = [80, 75, 82, 90, 85] }
    ];
}
```

### Bar Chart

```razor
<BarChart Title="Revenue by Quarter"
          Series="@revenue"
          XAxisLabels="@quarters"
          Stacked="false"
          Horizontal="false"
          CrosshairTooltip="true"
          Width="700" Height="350" />

@code {
    private List<string> quarters = ["Q1", "Q2", "Q3", "Q4"];

    private List<ChartSeries> revenue =
    [
        new() { Label = "Product A", Color = "#3B82F6", Values = [120, 145, 160, 180] },
        new() { Label = "Product B", Color = "#10B981", Values = [90, 110, 105, 130] }
    ];
}
```

### Donut Chart

```razor
<DonutChart Title="Sessions by Browser"
            Data="@browserData"
            MaxSegments="5"
            ShowLabels="true"
            ShowLegend="true"
            CenterLabel="Total"
            Width="700" Height="400" />

@code {
    private List<ChartSegment> browserData =
    [
        new() { Label = "Chrome", Value = 4521 },
        new() { Label = "Safari", Value = 1832 },
        new() { Label = "Firefox", Value = 987 },
        new() { Label = "Edge", Value = 645 }
    ];
}
```

### Pie Chart

```razor
<PieChart Title="Sessions by Device"
          Data="@deviceData"
          ShowLabels="true"
          ShowLegend="true"
          Width="700" Height="400" />
```

### World Map Chart

```razor
@using FactFoundry.Blazor.Charts.Geo

<WorldMapChart Title="Users by Country"
               Data="@countryData"
               ColorScale="@(new[] { "#dbeafe", "#2563eb" })"
               ShowLegend="true"
               Width="900" Height="450" />

@code {
    private List<MapDataPoint> countryData =
    [
        new() { CountryCode = "US", Value = 45000 },
        new() { CountryCode = "GB", Value = 12000 },
        new() { CountryCode = "DE", Value = 9500 },
        new() { CountryCode = "BR", Value = 5200 },
        new() { CountryCode = "JP", Value = 4200 }
    ];
}
```

Multi-stop color scales are supported:

```razor
<WorldMapChart ColorScale="@(new[] { "#fef08a", "#f97316", "#dc2626" })" ... />
```

### Bar List

A ranked "top N" breakdown — a bar fills behind each row, proportional to its value.
Use it for categorical breakdowns (top pages, browsers, countries) where a donut wastes
space. See [Building an analytics dashboard](docs/analytics-dashboard-patterns.md) for
the full pattern (KPI tiles, area-trend hero, and bar lists together).

```razor
<BarList Title="Top pages"
         Caption="by page views"
         Data="@pages"
         MaxItems="7"
         ShowShare="true"
         MoreText="View all pages"
         MoreHref="/analytics/pages" />

@code {
    private List<ChartSegment> pages =
    [
        new() { Label = "/", Value = 8 },
        new() { Label = "/docs/api", Value = 7 },
        new() { Label = "/download", Value = 6 },
        new() { Label = "/changelog", Value = 6 },
        new() { Label = "/features", Value = 5 }
    ];
}
```

### Commit Graph

A git-style branch/commit lane view — commits as dots, branches as color-coded lanes,
merges and branch points as curves. The component knows nothing about git; it takes any
DAG of nodes-with-parents (`CommitNode`) already in display order (newest first), which is
exactly what a LibGit2Sharp topological/time-sorted walk yields. Hover a row for the
author, short id and date; wire `OnCommitClick` to select a commit.

```razor
<CommitGraph Commits="@history" OnCommitClick="c => selected = c" />

@code {
    private List<CommitNode> history =
    [
        new() { Id = "9f3a1c", ParentIds = ["7b2d4f", "5c8e9a"], Message = "Merge feature/x",
                Author = "Kevin", Date = DateTimeOffset.Now, Refs = ["HEAD -> main"] },
        new() { Id = "5c8e9a", ParentIds = ["7b2d4f"], Message = "Feature work",
                Author = "Kevin", Refs = ["feature/x"] },
        new() { Id = "7b2d4f", ParentIds = [], Message = "Initial commit", Refs = ["tag: v1.0.0"] }
    ];
}
```

A ref prefixed `tag:` (the `git log --decorate` convention) is styled as a tag; every
other ref is styled as a branch.

### Calendar Heatmap

A GitHub-contributions-style grid — one cell per day in week columns, colored by each
day's value. Hand it a flat list of `CalendarPoint` (day + value); same-day points are
summed, and the grid spans the data's date range (or an explicit `StartDate`/`EndDate`).

```razor
<CalendarHeatmap Data="@days" ValueFormat="N0" OnDayClick="d => selected = d" />

@code {
    private List<CalendarPoint> days =
    [
        new() { Date = new DateOnly(2026, 1, 4), Value = 3 },
        new() { Date = new DateOnly(2026, 1, 5), Value = 7 },
        new() { Date = new DateOnly(2026, 1, 6), Value = 1 }
    ];
}
```

Tune it with `WeekStart`, `Levels` (intensity buckets), `Color`, `CellSize`/`CellGap`, and
the `ShowMonthLabels`/`ShowWeekdayLabels`/`ShowLegend`/`ShowTooltip` toggles.

## Theming

Wrap any section of your app with `ChartThemeProvider` to theme all charts within it:

```razor
@using FactFoundry.Blazor.Charts.Themes

<ChartThemeProvider Theme="ChartTheme.Dark">
    <LineChart ... />
    <BarChart ... />
    <WorldMapChart ... />
</ChartThemeProvider>
```

Built-in presets: `ChartTheme.Light` (default) and `ChartTheme.Dark`.

Create custom themes for full control:

```razor
@code {
    private ChartTheme custom = new()
    {
        TextColor = "#333333",
        GridColor = "#dddddd",
        GridOpacity = 0.2,
        TooltipBackground = "#2d2d2d",
        TooltipTextColor = "#ffffff",
        Palette = ["#ff6384", "#36a2eb", "#ffce56", "#4bc0c0"]
    };
}
```

Individual charts can override the cascading theme via the `Theme` parameter:

```razor
<LineChart Theme="@custom" ... />
```

## Responsive Sizing

All charts support fluid sizing via the `Responsive` parameter. When enabled, charts scale to fill their container width while maintaining aspect ratio — no JavaScript required:

```razor
<LineChart Responsive="true" Width="700" Height="350" Series="@series" ... />
```

`Width` and `Height` define the aspect ratio (viewBox) when responsive. The chart scales up/down to fit its parent container. Wrap in a `<div>` to constrain:

```razor
<div style="max-width: 600px;">
    <BarChart Responsive="true" Width="700" Height="350" ... />
</div>
```

## Label Auto-Thinning

When a chart has many data points (e.g., 30 days of data), X-axis labels are automatically thinned to prevent overlap. The algorithm estimates label widths and shows every Nth label to maintain readability.

To manually cap the number of visible labels:

```razor
<LineChart MaxXAxisLabels="10" XAxisLabels="@thirtyDayLabels" ... />
```

## Design Principles

- **Zero dependencies** — pure .NET and Blazor, nothing else
- **No JavaScript** — not even for tooltips or hover interactions
- **SVG rendering** — clean, scalable output via Razor component templates
- **Accessible** — SVG titles for screen readers
- **Themeable** — cascading theme provider with light/dark presets and full customization
- **Multi-target** — supports .NET 8.0, 9.0, and 10.0
- **Works everywhere** — Blazor Server and Blazor WebAssembly

## Target Frameworks

| Framework | Status |
|-----------|--------|
| .NET 8.0  | Supported |
| .NET 9.0  | Supported |
| .NET 10.0 | Supported |

## Project Structure

```
src/FactFoundry.Blazor.Charts/     # The library
samples/Charts.Sample.Server/      # Blazor Server demo app
samples/Charts.Sample.Wasm/        # Blazor WebAssembly demo app
tests/FactFoundry.Blazor.Charts.Tests/  # Unit tests (xUnit + bUnit)
docs/design/                       # Design documentation
tools/                             # Build/maintenance scripts
  convert_geojson.py               # Python script to regenerate WorldGeometry.cs
                                   #   from Natural Earth GeoJSON data (not a
                                   #   runtime dependency — code-gen only)
```

## Contributing

Contributions are welcome. Please open an issue first to discuss what you'd like to change.

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Open a pull request

## License

[MIT](LICENSE)
