# FactFoundry.Blazor.Charts

A zero-dependency, pure .NET charting library for Blazor applications. Charts are rendered as SVG via Razor components — no JavaScript, no interop, no external dependencies.

## Features

- **Line Chart** — single/multi-series, smooth curves, area fill, grid lines, tooltips
- **Donut Chart** — configurable inner radius, "Top N + Other" grouping, center label
- **Pie Chart** — proportional segments with hover highlighting

All charts include legends, hover tooltips, and accessible SVG output. Works in both Blazor Server and Blazor WebAssembly.

## Installation

```
dotnet add package FactFoundry.Blazor.Charts
```

## Quick Start

Add the using directive to your `_Imports.razor`:

```razor
@using FactFoundry.Blazor.Charts.Components
@using FactFoundry.Blazor.Charts.Models
```

### Line Chart

```razor
<LineChart Title="Sessions Over Time"
           Series="@seriesData"
           XAxisLabels="@dateLabels"
           ShowLegend="true"
           ShowGrid="true"
           Height="300" />

@code {
    private List<string> dateLabels = ["Mon", "Tue", "Wed", "Thu", "Fri"];

    private List<ChartSeries> seriesData =
    [
        new() { Label = "Chrome", Color = "#3B82F6", Values = [120, 135, 142, 128, 155] },
        new() { Label = "Firefox", Color = "#F59E0B", Values = [80, 75, 82, 90, 85] }
    ];
}
```

### Donut Chart

```razor
<DonutChart Title="Sessions by Browser"
            Data="@browserData"
            MaxSegments="5"
            ShowLegend="true"
            CenterLabel="Total"
            Height="300" />

@code {
    private List<ChartSegment> browserData =
    [
        new() { Label = "Chrome", Value = 4521 },
        new() { Label = "Safari", Value = 1832 },
        new() { Label = "Firefox", Value = 987 }
    ];
}
```

### Pie Chart

```razor
<PieChart Title="Sessions by Device"
          Data="@deviceData"
          ShowLegend="true"
          Height="300" />
```

## Design Principles

- **Zero dependencies** — pure .NET and Blazor only
- **No JavaScript** — not even for tooltips
- **Responsive** — charts adapt to specified dimensions
- **Accessible** — SVG titles for screen readers
- **Themeable** — colors configurable per-series or via default palette

## Targets

- .NET 8.0
- .NET 9.0
- .NET 10.0

## License

MIT
