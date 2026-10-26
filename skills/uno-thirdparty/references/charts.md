# Charts: LiveCharts2 and ScottPlot

Use **LiveCharts2** for business charts bound to data: bars, lines, areas, pies, with labels, legends and tooltips. Use **ScottPlot** for scientific or signal plots with thousands of points, driven from code. Both draw with SkiaSharp: set their colors with `skia-theme-colors.md` (in this skill's `references/`).

## LiveCharts2 with MVUX

```csharp
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

public partial record Sale(string Region, int Units);

// The chart's inputs travel together in one record.
public partial record SalesChart(ISeries[] Series, Axis[] XAxes);

public partial record SalesModel(ISalesService Sales)
{
    public IListFeed<Sale> Items => ListFeed.Async(Sales.GetAll);

    public IFeed<SalesChart> Chart => Items.AsFeed().Select(items => new SalesChart(
        [new ColumnSeries<int> { Name = "Units", Values = items.Select(s => s.Units).ToArray() }],
        [new Axis { Labels = items.Select(s => s.Region).ToArray() }]));
}
```

```xml
<Page xmlns:lvc="using:LiveChartsCore.SkiaSharpView.WinUI" ...>
  <lvc:CartesianChart x:Name="UnitsChart"
                      AutomationProperties.AutomationId="UnitsChart"
                      Series="{Binding Chart.Series}"
                      XAxes="{Binding Chart.XAxes}" />
</Page>
```

- **Never expose `ISeries[]` itself as a feed or state.** `IFeed<ISeries[]>` and `IState<ISeries[]>` bound to `Series` (directly or through a `FeedView`'s `{Binding Data}`) leave the chart empty with no binding error. The record wrapper above, a plain `ISeries[]` property, and an assignment from code-behind all render.
- A new feed value builds new series objects, so anything set on the old ones (colors) is lost: re-apply it when `Series` changes (`skia-theme-colors.md`).
- Other controls in the same namespace: `PieChart` (series are `PieSeries<T>`), `PolarChart`, `GeoMap`. Series types for `CartesianChart`: `ColumnSeries<T>`, `RowSeries<T>`, `LineSeries<T>`, `StackedColumnSeries<T>`, `ScatterSeries<T>`, `HeatSeries<T>`.
- Category labels go on the axis (`Axis.Labels`); without them the X axis shows 0, 1, 2.
- `CartesianChart` has no intrinsic size: give it a row with `*` height, or a `Height`.

## ScottPlot

ScottPlot has no bindable data surface. Fill the plot from code-behind and call `Refresh()` after every change.

```xml
<Page xmlns:scottplot="using:ScottPlot.WinUI" ...>
  <scottplot:WinUIPlot x:Name="TrendPlot" AutomationProperties.AutomationId="TrendPlot" />
</Page>
```

```csharp
TrendPlot.Plot.Add.Signal(samples);           // double[]; also Add.Scatter(xs, ys), Add.Bars(values)
TrendPlot.Plot.Axes.Title.Label.Text = "Load";
TrendPlot.Refresh();
```

ScottPlot suits data the page computes or loads itself. For data that lives in an MVUX feed, use LiveCharts2, which binds.

## Checks before you finish

- The chart shows the expected number of bars or points in the running app; a compile-clean chart can be empty.
- The axis labels and series colors stay readable after switching to dark mode.

## Sources

- LiveCharts2: the `beto-rodriguez/LiveCharts2` repository, `samples/` (Uno WinUI sample).
- ScottPlot: scottplot.net, the Uno Platform quickstart.
