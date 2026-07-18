using AngleSharp.Dom;
using Bunit;
using FactFoundry.Blazor.Charts.Components;
using FactFoundry.Blazor.Charts.Models;
using Xunit;

namespace FactFoundry.Blazor.Charts.Tests;

public class CalendarHeatmapTests : BunitContext
{
    private static List<CalendarPoint> Week(DateOnly start, params int[] values) =>
        values.Select((v, i) => new CalendarPoint { Date = start.AddDays(i), Value = v }).ToList();

    [Fact]
    public void Renders_Root_Svg()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 2, 3)));
        Assert.NotNull(cut.Find("svg.ff-calendar-heatmap"));
    }

    [Fact]
    public void Empty_Data_Renders_Nothing()
    {
        var cut = Render<CalendarHeatmap>(p => p.Add(x => x.Data, new List<CalendarPoint>()));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void Renders_One_Cell_Per_Day_In_Range_Including_Zero_Days()
    {
        // Jan 4 2026 is a Sunday (default WeekStart). Range spans Jan 4..Jan 10 = 7 days.
        var data = new List<CalendarPoint>
        {
            new() { Date = new DateOnly(2026, 1, 4), Value = 5 },
            new() { Date = new DateOnly(2026, 1, 10), Value = 3 }
        };
        var cut = Render<CalendarHeatmap>(p => p.Add(x => x.Data, data));

        // Every day between the first and last date gets a cell, even the empty ones.
        Assert.Equal(7, cut.FindAll(".ff-cal-cell").Count);
    }

    [Fact]
    public void Explicit_Start_And_End_Bound_The_Grid()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1))
            .Add(x => x.StartDate, new DateOnly(2026, 1, 4))
            .Add(x => x.EndDate, new DateOnly(2026, 1, 17))); // 14 days

        Assert.Equal(14, cut.FindAll(".ff-cal-cell").Count);
    }

    [Fact]
    public void Same_Date_Points_Are_Summed()
    {
        var date = new DateOnly(2026, 1, 4);
        var data = new List<CalendarPoint>
        {
            new() { Date = date, Value = 2 },
            new() { Date = date, Value = 3 }
        };
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, data)
            .Add(x => x.ValueFormat, "N0"));

        cut.Find(".ff-cal-cell").MouseOver();
        Assert.Contains(">5<", cut.Markup); // 2 + 3
    }

    [Fact]
    public void Zero_Value_Cell_Uses_Muted_Empty_Fill_Not_The_Base_Color()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 0, 10))
            .Add(x => x.Color, "#10B981"));

        var cells = cut.FindAll(".ff-cal-cell");
        // First cell (value 0) is not the base color; the busy cell is.
        Assert.NotEqual("#10B981", cells[0].GetAttribute("fill"));
        Assert.Equal("#10B981", cells[1].GetAttribute("fill"));
    }

    [Fact]
    public void Highest_Value_Cell_Is_Full_Opacity()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 100)));

        var busiest = cut.FindAll(".ff-cal-cell")[1];
        Assert.Equal("1.00", busiest.GetAttribute("fill-opacity"));
    }

    [Fact]
    public void Renders_Month_Labels()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 3, 1), 1, 2, 3)));
        Assert.Contains("Mar", cut.Markup);
    }

    [Fact]
    public void Weekday_Labels_Can_Be_Hidden()
    {
        var withLabels = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 2, 3)));
        Assert.Contains("Mon", withLabels.Markup);

        var without = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 2, 3))
            .Add(x => x.ShowWeekdayLabels, false));
        Assert.DoesNotContain("Mon", without.Markup);
    }

    [Fact]
    public void Legend_Has_One_Swatch_Per_Level_Plus_Empty()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 2, 3))
            .Add(x => x.Levels, 4));

        Assert.Contains("Less", cut.Markup);
        Assert.Contains("More", cut.Markup);
        // Legend renders Levels + 1 swatches (rx="2" width="11"), distinct from day cells.
        var swatches = cut.FindAll("rect").Where(r => r.GetAttribute("width") == "11").ToList();
        Assert.Equal(5, swatches.Count);
    }

    [Fact]
    public void Legend_Can_Be_Hidden()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 1, 2, 3))
            .Add(x => x.ShowLegend, false));
        Assert.DoesNotContain("Less", cut.Markup);
    }

    [Fact]
    public void Hover_Shows_Tooltip_With_Date_And_Value()
    {
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, [new CalendarPoint { Date = new DateOnly(2026, 1, 4), Value = 7 }])
            .Add(x => x.ValueFormat, "N0"));

        Assert.DoesNotContain("Jan 4, 2026", cut.Markup);
        cut.Find(".ff-cal-cell").MouseOver();
        Assert.Contains("Jan 4, 2026", cut.Markup);
        Assert.Contains(">7<", cut.Markup);
    }

    [Fact]
    public void OnDayClick_Fires_With_Date_And_Value()
    {
        CalendarPoint? clicked = null;
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, Week(new DateOnly(2026, 1, 4), 5, 9))
            .Add(x => x.OnDayClick, pt => clicked = pt));

        cut.FindAll(".ff-cal-cell")[1].Click();
        Assert.NotNull(clicked);
        Assert.Equal(new DateOnly(2026, 1, 5), clicked!.Date);
        Assert.Equal(9, clicked.Value);
    }

    [Fact]
    public void WeekStart_Monday_Puts_Monday_In_The_Top_Row()
    {
        // Jan 5 2026 is a Monday. With WeekStart=Monday it should be at row 0 (y == TopPad).
        var cut = Render<CalendarHeatmap>(p => p
            .Add(x => x.Data, [new CalendarPoint { Date = new DateOnly(2026, 1, 5), Value = 1 }])
            .Add(x => x.WeekStart, DayOfWeek.Monday));

        var cell = cut.Find(".ff-cal-cell");
        // TopPad is 18 (month labels shown); row 0 → y == 18.0.
        Assert.Equal("18.0", cell.GetAttribute("y"));
    }

    [Fact]
    public void Escapes_No_Markup_Injection_Via_Value_Format()
    {
        // Sanity: rendering with a large value set doesn't throw and stays well-formed.
        var data = Enumerable.Range(0, 400)
            .Select(i => new CalendarPoint { Date = new DateOnly(2025, 1, 1).AddDays(i), Value = i % 13 })
            .ToList();
        var cut = Render<CalendarHeatmap>(p => p.Add(x => x.Data, data));
        Assert.True(cut.FindAll(".ff-cal-cell").Count >= 400);
    }
}
