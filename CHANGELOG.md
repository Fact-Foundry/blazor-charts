# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-07-18

### Fixed

- **CommitGraph** — ref decorations now split into separate badges: a `HEAD -> main` (or comma-joined) decoration renders as distinct `HEAD` and `main` badges instead of one arrowed label, so consumers can pass raw `git log --decorate` strings and get clean output.
- **CommitGraph** — a long commit message no longer stretches the hover tooltip across the whole panel and buries the rows behind it (reported against 1.0.3). The message now word-wraps to a bounded width over up to six lines (configurable via `TooltipMaxLines`) before ellipsizing, with the author and short-id/date beneath as meta rows; embedded newlines and runs of whitespace are collapsed, and long tokens (hashes, `snake_case` ids) hard-break instead of overflowing. The box is additionally clamped to the chart's own width, so it can never be clipped at the edge even when `TooltipMaxWidth` exceeds a narrow chart.

### Added

- **Accessible `<title>` + `<desc>` on every SVG chart** — `LineChart`, `BarChart`, `PieChart`, `DonutChart`, `WorldMapChart`, `Sparkline`, `CommitGraph`, and `CalendarHeatmap` now emit an accessible name (`<title>`) and a data-aware description (`<desc>`) wired through `role="img"` + `aria-labelledby`, so a screen reader announces what the chart shows (e.g. _"Pie chart of 2 segments. Largest is Chrome at 60%."_). A new `Description` parameter overrides the auto-generated text; the accessible name uses `Title` when set, otherwise the chart type.
- **CalendarHeatmap component** — a GitHub-contributions-style calendar heatmap rendered as pure Razor-to-SVG and themed through `ChartThemeProvider`. Takes a flat list of `CalendarPoint` (day + value, same-day points summed) and lays it out as week columns × weekday rows, coloring each cell by intensity bucket. Month and weekday labels, a "Less … More" legend, and a themed hover tooltip (date + value); `OnDayClick` reports the selected day. Params: `Data`, `StartDate`, `EndDate`, `WeekStart`, `CellSize`, `CellGap`, `CellRadius`, `Levels`, `Color`, `ShowMonthLabels`, `ShowWeekdayLabels`, `ShowLegend`, `ShowTooltip`, `DateFormat`, `ValueFormat`, `Responsive`, `OnDayClick`.
- **File-change stats in the `CommitGraph` tooltip** — optional `FilesChanged`, `Insertions`, and `Deletions` fields on `CommitNode` (mapping directly to LibGit2Sharp's `Patch.FilesChanged`/`LinesAdded`/`LinesDeleted`) render a GitLens-style stats row — e.g. `16 files changed  +757  -78` with insertions green and deletions red. The row is omitted entirely when no stat field is set.
- **`TooltipMaxWidth` and `TooltipMaxLines` parameters on `CommitGraph`** — cap the hover tooltip's width (SVG units, default 360) and wrapped-message line count (default 6); longer messages wrap and then ellipsize to fit rather than widening or lengthening the box unboundedly.

### Changed

- **CommitGraph** tooltip date now defaults to a month-abbreviated format (`MMM d, yyyy HH:mm`, e.g. "Jul 15, 2026 07:48") instead of `yyyy-MM-dd HH:mm`, so it reads unambiguously regardless of the viewer's locale. Override via `DateFormat` as before.

## [1.0.3] - 2026-07-17

### Added

- **CommitGraph component** — a git-style branch/commit lane view (the idiom of VS Code's Git Graph, GitKraken, gitk), rendered as pure Razor-to-SVG and themed through `ChartThemeProvider`. Takes any DAG of `CommitNode` (id/parents/refs plus message/author/date) already in display order — the shape a LibGit2Sharp walk yields — and runs a single lane-assignment pass: commits become color-coded lane dots, merges hollow rings, branch/merge links bezier curves between lanes. Refs render as badges (`tag:`-prefixed → tag styling, else branch); hovering a row shows a themed tooltip with author, short id and date; `OnCommitClick` reports the selected commit. Params: `Commits`, `RowHeight`, `LaneWidth`, `DotRadius`, `Width`, `ShowMessage`, `ShowRefs`, `ShowTooltip`, `ShortIdLength`, `DateFormat`, `Responsive`, `OnCommitClick`.
- **Sparkline component** — a bare inline trend (area + line, no axes/labels/legend) for KPI tiles. Reserves no axis chrome, so it stays legible at ~80×30 where a shrunk `LineChart` collapses. Params: `Values`, `Color`, `ShowArea`, `ShowEndDot`, `StrokeWidth`, `Responsive`.
- **`LinkColor` parameter on `BarList`** — colors the trailing "view all →" link independently of the bar fill. Unset, it inherits `AccentColor` (unchanged behavior), so the drill link can be a warm action color while the bars stay a cool data color.

### Docs

- Corrected `docs/analytics-dashboard-patterns.md` §1 to use `Sparkline` for KPI tiles (a shrunk `LineChart` does not work at small sizes because of its fixed axis padding).

## [1.0.2] - 2026-07-09

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
