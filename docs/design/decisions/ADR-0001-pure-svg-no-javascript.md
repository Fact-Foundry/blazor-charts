# ADR-0001: Pure SVG Rendering Without JavaScript

> **Status:** Accepted
> **Date:** 2026-06-07
> **Deciders:** Kevin Williams

## Context

Every mature charting library in the Blazor ecosystem (Radzen Charts, ApexCharts.Blazor, ChartJs.Blazor) wraps a JavaScript library. This creates:

- Mandatory JS dependencies that increase bundle size
- JavaScript interop overhead on every render and interaction
- Conflicts with strict Content Security Policies (no inline scripts)
- Complexity in server-side rendering and pre-rendering scenarios
- External dependency supply chain risk

TelemetryForge needs charts for its analytics dashboard but follows the zero-dependency philosophy established by OpenStandardLibrary.

## Decision

Render all charts as pure SVG via Razor components. No JavaScript. No interop. Interactions (hover, tooltips) are handled entirely through Blazor's event system (`@onmouseover`, `@onmouseout`).

SVG is the rendering target because:
- Native to all browsers, no runtime required
- Scalable and resolution-independent
- Accessible (supports `<title>`, `<desc>`, ARIA)
- Styleable via CSS
- Serializable (can be exported/saved as-is)

## Consequences

### Positive

- Zero JavaScript dependencies — CSP-compatible out of the box
- Works identically in Server and WebAssembly modes
- No interop latency on interactions
- Pre-renderable for static SSR scenarios
- No external supply chain risk

### Negative

- Hover/interaction latency in Server mode depends on SignalR round-trip
- Large datasets (thousands of points) may produce large SVG DOM trees
- Some animation patterns (transitions, morphing) are harder without JS
- Tooltip positioning is constrained to SVG coordinate space

### Risks

- Performance ceiling for very large datasets — mitigation: data sampling/decimation in future versions
- SVG `<text>` element conflicts with Razor's `@text` directive — mitigated via `MarkupString` rendering

## Alternatives Considered

### JavaScript wrapper (ApexCharts, Chart.js)

Proven ecosystem, rich interactivity, but violates the zero-dependency constraint and introduces interop complexity.

### HTML Canvas via JS interop

Better performance for large datasets, but requires JavaScript and loses SVG benefits (accessibility, scalability, serializability).

### Blazor Canvas (SkiaSharp / Canvas API)

Pure .NET rendering to canvas, but requires SkiaSharp dependency (large native binary) and loses SVG's DOM-based interactivity.

## References

- [Project prompt](../../prompts/factfoundry-blazor-charts-project.md)
- [Design document](../DESIGN.md)
