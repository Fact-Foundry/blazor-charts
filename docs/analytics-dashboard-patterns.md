# Building an analytics dashboard

Notes and copy-paste examples for assembling a professional analytics overview —
the kind of page GA4, Cloudflare, and Power BI present — from this library.

The guiding idea: **a dashboard is scanned, not read.** Lead every card with a
headline number, back it with one restrained trend, and use ranked bar lists for
"top N" breakdowns instead of donuts. One accent color, muted gridlines, tabular
numbers. That restraint is what reads as "professional".

This page covers three building blocks:

1. **KPI tile with a sparkline** — a big number + a tiny area trend (`LineChart` preset)
2. **Area-trend hero** — the one prominent line chart (`LineChart` with `ShowArea`)
3. **Ranked bar list** — the breakdown cards (`BarList`, new)

All three work in Blazor Server and WebAssembly, need no JavaScript, and honor the
cascading `ChartThemeProvider`.

---

## 1. KPI tile with a sparkline

Use the `Sparkline` component — a bare area + line with no axes, labels, grid, or
legend. Do **not** try to shrink a `LineChart` for this: `LineChart` reserves ~50px on
the left for axis labels, so below ~200px wide the plot collapses and the tick labels
bleed into the tile. `Sparkline` reserves no chrome and stays legible at 80×30.

```razor
<div class="kpi">
    <span class="kpi-label">Page views</span>
    <div class="kpi-row">
        <span class="kpi-value">44</span>
        <div class="kpi-spark">
            <Sparkline Values="@spark" Color="#4f8bff" Width="82" Height="30" />
        </div>
    </div>
</div>

@code {
    // Just the shape of the last N periods.
    private List<decimal> spark = [9, 12, 10, 14, 16, 13, 22];
}
```

```css
.kpi { display: flex; flex-direction: column; gap: 7px;
       background: var(--card); border: 1px solid var(--border);
       border-radius: 12px; padding: 13px 14px; }
.kpi-label { font-size: .72rem; letter-spacing: .06em; text-transform: uppercase; color: var(--muted); }
.kpi-row { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.kpi-value { font-size: 1.85rem; font-weight: 680; letter-spacing: -.02em; font-variant-numeric: tabular-nums; }
.kpi-spark { width: 78px; }
```

**Knobs:** `ShowArea` (default on) draws the gradient fill; `ShowEndDot` (default on)
marks the latest point; `StrokeWidth` defaults to `1.6`; `Color` pins the accent
(otherwise the theme's first palette color is used). It's `Responsive` by default —
give the wrapping element a width.

---

## 2. Area-trend hero

The single prominent chart. Same `LineChart`, but full size, with a faint grid and
one or two area-filled series. Give it a dark, low-contrast theme so the data — not
the frame — carries the card.

```razor
<div class="card">
    <div class="card-head">
        <div>
            <h2>Traffic over time</h2>
            <p class="cap">Page views and sessions, hourly</p>
        </div>
    </div>
    <LineChart Series="@traffic" XAxisLabels="@hours"
               Responsive="true" Width="1000" Height="250"
               ShowArea="true" SmoothLines="true" ShowGrid="true"
               ShowLegend="true" StrokeWidth="2"
               MaxXAxisLabels="6"
               Theme="@trendTheme" />
</div>

@code {
    private List<string> hours = ["", "4 AM", "8 AM", "12 PM", "4 PM", "8 PM", ""];

    private List<ChartSeries> traffic =
    [
        new() { Label = "Page views", Color = "#4f8bff", Values = [6, 13, 19, 14, 25, 12, 8] },
        new() { Label = "Sessions",   Color = "#34c38f", Values = [2,  4,  5,  4,  6,  3, 2] }
    ];

    // A quiet theme: faint grid, muted labels — the mock's look.
    private ChartTheme trendTheme = new()
    {
        TextColor = "#8a95a6",
        GridColor = "#8a95a6",
        GridOpacity = 0.12,
        LabelOpacity = 0.6,
        Palette = ["#4f8bff", "#34c38f"]
    };
}
```

**Knobs:** `SmoothLines` toggles curved vs. straight segments; `MaxXAxisLabels` thins
crowded axes; lower `GridOpacity` for a subtler grid. Put the accent first in `Palette`
so single-series charts pick it up automatically.

---

## 3. Ranked bar list — `BarList`

The breakdown cards. `BarList` ranks `ChartSegment` data descending and draws a bar
*behind* each row, proportional to the value. Use it for top pages, browsers, OS,
countries — anywhere a donut would waste space.

```razor
<div class="card">
    <BarList Title="Top pages" Caption="by page views"
             Data="@pages" MaxItems="7"
             MoreText="View all pages" MoreHref="/analytics/pages" />
</div>

@code {
    private List<ChartSegment> pages =
    [
        new() { Label = "/",            Value = 8 },
        new() { Label = "/docs/api",    Value = 7 },
        new() { Label = "/download",    Value = 6 },
        new() { Label = "/changelog",   Value = 6 },
        new() { Label = "/features",    Value = 5 },
        new() { Label = "/contact",     Value = 4 },
        new() { Label = "/docs/getting-started", Value = 3 }
    ];
}
```

### Show a share percentage

```razor
<BarList Title="Browsers" Data="@browsers" ShowShare="true" />
@* renders: Chrome 125   5 · 56% *@
```

### Status list with colored dots

Set `ShowDot="true"` and give each segment a `Color`. The dot uses the segment color;
the bar stays a neutral accent. Ideal for a visitor-identity or severity breakdown.

```razor
<BarList Title="Visitor identity" Caption="this period"
         Data="@identity" ShowDot="true"
         MoreText="Open Security" MoreHref="/security" />

@code {
    private List<ChartSegment> identity =
    [
        new() { Label = "Human",          Value = 9, Color = "#34c38f" },
        new() { Label = "Verified bot",   Value = 1, Color = "#4f8bff" },
        new() { Label = "Unknown bot",    Value = 2, Color = "#e2a13a" },
        new() { Label = "Security event", Value = 2, Color = "#e2a13a" }
    ];
}
```

### Parameters

| Parameter | Type | Default | Purpose |
|---|---|---|---|
| `Data` | `List<ChartSegment>` | `[]` | Rows to rank (sorted by `Value` desc). |
| `Title` | `string?` | — | Heading above the list. |
| `Caption` | `string?` | — | Sub-label under the title. |
| `MaxItems` | `int` | `7` | Cap on rows shown. |
| `ShowShare` | `bool` | `false` | Append each value's % of the total. |
| `ValueFormat` | `string?` | `"0.##"` | .NET numeric format for the value. |
| `ShowDot` | `bool` | `false` | Colored dot per row (uses `ChartSegment.Color`). |
| `AccentColor` | `string?` | theme color 0 | Bar fill color. |
| `BarOpacity` | `double` | `0.16` | Fill opacity behind rows. |
| `MoreText` / `MoreHref` | `string?` | — | Trailing "view all →" link. |
| `LinkColor` | `string?` | inherits `AccentColor` | Own color for the "more" link. |
| `Theme` | `ChartTheme?` | cascaded | Text/label colors. |

Give the drill link its own accent while the bars stay a cool data color — e.g. warm
action links over cool bars:

```razor
<BarList Title="Top pages" Data="@pages"
         AccentColor="#12DD93"           @* bars: cool data green *@
         LinkColor="#FF6A1F"             @* link: warm action ember *@
         MoreText="View all pages" MoreHref="/analytics/pages" />
```

> A warm color at low opacity over a dark ground reads brown, so keep **bar fills cool**
> and spend the warm accent on the **link** (an action), not the fill.

**Styling:** `BarList` ships its own scoped styles under the `ff-barlist-*` classes.
Every color comes from the resolved theme or the parameters above — to restyle globally,
wrap the page in a `ChartThemeProvider`, or override the `ff-barlist-*` classes in your
app CSS (they have low specificity by design).

---

## Putting it together

The overview page is: a KPI strip (five tiles from §1) → one hero card (§2) → a
responsive grid of `BarList` cards (§3), each linking to a drill-through report. Keep
the card frame (border, radius, padding) consistent across all three and let the data
do the rest.

See the TelemetryForge Analytics page for a full reference implementation.
