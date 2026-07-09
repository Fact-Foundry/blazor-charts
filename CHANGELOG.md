# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **BarList component** — a ranked "top N" breakdown list; a bar fills behind each row proportional to its value. Supports share percentages (`ShowShare`), colored status dots (`ShowDot`), a trailing "view all →" link, and full theming. The scannable dashboard idiom for categorical breakdowns (top pages, browsers, countries) where a donut wastes space.
- **docs/analytics-dashboard-patterns.md** — guide to assembling a GA4/Cloudflare-style analytics overview (KPI sparkline tiles, area-trend hero, bar-list breakdowns) from the library.

## [1.0.1] - 2026-06-08

### Fixed

- Hover tooltips now paint above the legend instead of the legend bleeding through them (LineChart, BarChart, DonutChart, PieChart, WorldMapChart)
- Hover tooltips no longer flicker when positioned under the cursor — tooltip elements are now `pointer-events: none` so the mouse passes through to the hover target (all chart types)
- LineChart/BarChart crosshair tooltip box now sizes to its widest line so long series labels are no longer clipped
- DonutChart/PieChart hover tooltip is kept within the chart bounds instead of being clipped at the edges; it shifts to stay on-screen even when a label is wider than the plotting area
- WorldMapChart tooltip now appears at the hovered country's centroid instead of always at the map center (the path-coordinate parser ignored the SVG command letters glued to each first coordinate)

### Added

- DonutChart hover tooltip gained a background box (matching the line/bar tooltips) for readability
- **ShowLegendPercent** parameter on DonutChart — appends each segment's percentage to its legend entry

## [1.0.0] - 2026-06-07

### Added

- **LineChart** component — time-series line chart with SVG rendering
  - Single and multi-series support
  - Configurable smooth (cubic bezier) or straight lines
  - Optional area fill under lines
  - Grid lines, axis labels, and legend
  - Hover tooltips on data points
- **DonutChart** component — proportional donut chart
  - Configurable inner radius (donut hole size)
  - "Top N + Other" grouping via `MaxSegments`
  - Center label with total count display
  - Hover highlighting and tooltips
  - Legend with color swatches
- **PieChart** component — proportional pie chart
  - All donut features minus inner radius
  - Segment hover highlighting
  - Legend support
- **WorldMapChart** component — choropleth world map heatmap
  - 174 countries with Natural Earth 110m boundaries (equirectangular projection)
  - Data-driven color fill via configurable multi-stop color scale
  - Hover tooltips showing country name and value
  - Gradient legend with min/max labels
  - Supports 2-color and 3-color (or more) gradient scales
  - No-data countries rendered in neutral gray
  - Theme-aware (text, grid/stroke, tooltip colors)
- **ChartTheme** system — cascading theme support for all chart components
  - Built-in `ChartTheme.Light` and `ChartTheme.Dark` presets
  - `ChartThemeProvider` component delivers theme to all child charts via CascadingValue
  - Per-chart `Theme` parameter override for one-off customization
  - Configurable: text color, grid color/opacity, crosshair color/opacity, tooltip background/opacity/text, label opacity, and color palette
  - Custom palettes via `Palette` property override the default 12-color palette
- **BarChart** component — grouped/stacked bar chart
  - Vertical and horizontal orientations
  - Grouped (side-by-side) and stacked modes
  - Single and multi-series support
  - Grid lines, axis labels, and legend
  - Hover tooltips on individual bars
  - Crosshair tooltip mode (shows all series values for a category)
- **ShowLabels** parameter on PieChart and DonutChart
  - Leader line callout labels from each slice edge
  - Displays "Label: Value (Pct%)" format
  - Automatically adjusts chart radius to fit labels
  - Hover tooltip suppressed when labels are visible
- **X-axis label auto-thinning** — prevents label crowding on dense data sets
  - Automatically calculates how many labels fit without overlapping based on label length and chart width
  - Optional `MaxXAxisLabels` parameter to manually cap the number of visible labels
  - Applies to both LineChart and BarChart (vertical and horizontal orientations)
  - Shows evenly spaced labels (every Nth) to maintain readability
  - Always renders the last (most recent) label; suppresses second-to-last if it would collide
- **Legend wrapping** — multi-row legend layout for charts with many series
  - Automatically wraps legend items to additional rows when they exceed chart width
  - Chart area height adjusts to accommodate multi-row legends
  - Applies to both LineChart and BarChart
- **Responsive auto-sizing** — `Responsive` parameter on all chart components
  - Sets SVG `width="100%"` with `viewBox` and `preserveAspectRatio="xMidYMid meet"`
  - Charts scale fluidly to fill their container while maintaining aspect ratio
  - No JavaScript required — pure SVG responsive behavior
  - Defaults to `false` (fixed dimensions) for backward compatibility
- **Data models**: `ChartSeries` (line charts), `ChartSegment` (pie/donut charts)
- **Default color palette**: 12 colors that work on light and dark backgrounds
- **Multi-target**: .NET 8.0, 9.0, and 10.0
- Sample apps for both Blazor Server and Blazor WebAssembly
- Unit tests using bUnit
- GitHub Actions workflow for NuGet publishing on tag push
