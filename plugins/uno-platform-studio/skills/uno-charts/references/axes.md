# Axes and labels

Category labels, value formats, date axes and the values printed on bars, for LiveCharts2 (`livecharts.md`).

**Contents:** [Categories and values](#categories-and-values) · [Dates](#dates) · [Values on the bars](#values-on-the-bars)

## Categories and values

- Category names go in `Axis.Labels`; the series holds numbers in the same order.
- Format values with `Labeler`: `new Axis { Labeler = v => v.ToString("C0") }`. `YAxes` starts at the data minimum: set `MinLimit = 0` for bars and amounts.
- Name each series (`Name = "Income"`): the legend and tooltip show it. Show the legend (`LegendPosition="Bottom"`) for two or more series, hide it for one.
- An axis with `Labels` drops every other label when they don't fit. `MinStep = 1, ForceStepToMin = true` keeps every one. Category names longer than a word fit better as a `RowSeries<double>` (horizontal bars) with the names on the Y axis and `IsInverted = true` to put the first one on top.

## Dates

`Labels` is for a dozen categories at most, such as month names. Daily or weekly data is `DateTimePoint` values on a date axis: 90 strings in `Labels` overlap into a smear.

```csharp
using LiveChartsCore.Defaults;

new LineSeries<DateTimePoint> { Values = days.Select(d => new DateTimePoint(d.Date, d.Orders)).ToArray(), GeometrySize = 0 }

new Axis
{
    UnitWidth = TimeSpan.FromDays(1).Ticks,
    MinStep = TimeSpan.FromDays(14).Ticks,   // a label every two weeks; 28 days fits a phone-width chart
    ForceStepToMin = true,
    // The axis also asks for values outside any date (while it pads its limits): new DateTime() throws on those and the chart draws nothing.
    Labeler = v => v is >= 0 and < 3.155e18 ? new DateTime((long)v).ToString("MMM d") : "",
}
```

## Values on the bars

Data labels are off by default. Turn them on in the Model with an empty paint, which `ChartTheme` colors from the theme (`theme-colors.md`), and leave room past the longest bar on the value axis, or its label is clipped at the plot edge:

```csharp
new RowSeries<double>
{
    Values = totals,
    DataLabelsPaint = new SolidColorPaint(),   // using LiveChartsCore.SkiaSharpView.Painting
    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue / 1000:0}k",
}

new Axis { MinLimit = 0, MaxLimit = totals.Max() * 1.25 }   // the value axis: XAxes for a RowSeries
```
