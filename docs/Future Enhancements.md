# Future Enhancements

Items that have been intentionally removed or deferred, organized by release target.

---

# Pre-Release (v1.0.0)

These items must be completed before the initial public release.

- Responsive sizing (charts adapt to container width)
- Accessibility: SVG `<desc>` elements with meaningful descriptions
- X-axis label rotation for long labels

---

# Post v1 — Phase 1

- **Bar Chart** (vertical and horizontal orientations)
- **Area Chart** (filled line chart variant, standalone component)
- **Sparkline** (minimal inline line chart for dashboard stat cards)
- CSS variable theming (colors driven by `--chart-*` custom properties)
- Animation support (segment transitions on data change)
- Custom tooltip templates via `RenderFragment`

---

# Post v1 — Phase 2

- **Stacked Bar Chart**
- **Heatmap** (session activity by hour/day matrix)
- **World Map** — SVG world map with country paths filled by session volume using ISO country codes; color scale reflects traffic intensity per country
- Data point decimation for large datasets (1000+ points)
- Export chart as SVG file
- Click events on data points/segments
