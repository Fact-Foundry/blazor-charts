# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
- **ShowLabels** parameter on PieChart and DonutChart
  - Leader line callout labels from each slice edge
  - Displays "Label: Value (Pct%)" format
  - Automatically adjusts chart radius to fit labels
  - Hover tooltip suppressed when labels are visible
- **Data models**: `ChartSeries` (line charts), `ChartSegment` (pie/donut charts)
- **Default color palette**: 12 colors that work on light and dark backgrounds
- **Multi-target**: .NET 8.0, 9.0, and 10.0
- Sample apps for both Blazor Server and Blazor WebAssembly
- Unit tests using bUnit
- GitHub Actions workflow for NuGet publishing on tag push
