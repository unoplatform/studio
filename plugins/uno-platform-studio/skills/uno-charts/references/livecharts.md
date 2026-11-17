# LiveCharts2

`LiveChartsCore.SkiaSharpView.Uno.WinUI`, XAML prefix `xmlns:lvc="using:LiveChartsCore.SkiaSharpView.WinUI"`. Install it first (`install.md`).

**Contents:** [Bound to MVUX](#bound-to-mvux) · [Axes, labels and values](#axes-labels-and-values) · [Pie and donut](#pie-and-donut) · [Live updates](#live-updates) · [Your record as a point](#your-record-as-a-point) · [Checks](#checks)

| Control | Series types |
|---------|--------------|
| `CartesianChart` | `LineSeries<T>`, `ColumnSeries<T>`, `RowSeries<T>` (horizontal bars), `StackedColumnSeries<T>`, `StackedAreaSeries<T>`, `ScatterSeries<T>`, `StepLineSeries<T>`, `CandlesticksSeries<FinancialPoint>`, `HeatSeries<T>` |
| `PieChart` | `PieSeries<T>`: one series per slice; `InnerRadius` makes a donut; a gauge is a `PieChart` with `MaxValue` and a single series |
| `PolarChart` | `PolarLineSeries<T>`: radar charts |

`T` is `double` or `int` (or your record with a mapper, below). Area charts are `LineSeries<T>` with a `Fill`; a plain line sets `Fill = null`.

## Bound to MVUX

The chart's inputs travel together in one record, built from the data feed. The service returns the numbers already grouped; the Model only turns them into series.

```csharp
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace MyApp.Presentation;

public partial record MonthlyTotal(string Month, double Amount);

public partial record SpendChart(ISeries[] Series, Axis[] XAxes, Axis[] YAxes);

public partial record DashboardModel(IReportService Reports)
{
    public IListFeed<MonthlyTotal> Totals => ListFeed.Async(Reports.GetMonthlyTotals);

    public IFeed<SpendChart> Chart => Totals.AsFeed().Select(items => new SpendChart(
        [new ColumnSeries<double> { Name = "Spend", Values = items.Select(t => t.Amount).ToArray() }],
        [new Axis { Labels = items.Select(t => t.Month).ToArray() }],
        [new Axis { MinLimit = 0 }]));
}
```

The chart sits in its own `FeedView`, which gives it loading, error and empty states (the `uno-mvux` skill, `references/feedview.md`); inside the template the record is `Data`.

```xml
<Page xmlns:lvc="using:LiveChartsCore.SkiaSharpView.WinUI"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI" ...>
  <mvux:FeedView Source="{Binding Chart}">
    <DataTemplate>
      <lvc:CartesianChart AutomationProperties.AutomationId="SpendChart"
                          local:ChartTheme.FollowTheme="True"
                          Series="{Binding Data.Series}"
                          XAxes="{Binding Data.XAxes}"
                          YAxes="{Binding Data.YAxes}"
                          LegendPosition="Hidden" />
    </DataTemplate>
  </mvux:FeedView>
</Page>
```

- **Never `IFeed<ISeries[]>` or `IState<ISeries[]>` bound to `Series`**, directly or as `{Binding Data}` in a `FeedView`: the chart stays empty with no binding error. Bind through the record.
- `local:ChartTheme.FollowTheme` paints the chart from the theme, again on every new feed value (`theme-colors.md`).

## Axes, labels and values

- Category names go in `Axis.Labels`; the series holds numbers in the same order.
- Format values with `Labeler`: `new Axis { Labeler = v => v.ToString("C0") }`. `YAxes` starts at the data minimum: set `MinLimit = 0` for bars and amounts.
- Name each series (`Name = "Income"`): the legend and tooltip show it. Show the legend (`LegendPosition="Bottom"`) for two or more series, hide it for one.
- Dates on the X axis: either use the month names as `Labels`, or `DateTimePoint` values with `Labeler = v => new DateTime((long)v).ToString("MMM")` and `UnitWidth = TimeSpan.FromDays(1).Ticks`.

## Pie and donut

```csharp
public partial record CategoryChart(ISeries[] Slices);

public IFeed<CategoryChart> ByCategory => Categories.AsFeed().Select(items => new CategoryChart(
    items.Select(c => (ISeries)new PieSeries<double> { Name = c.Name, Values = [c.Amount], InnerRadius = 60 }).ToArray()));
```

```xml
<lvc:PieChart Series="{Binding ByCategory.Slices}" LegendPosition="Right" />
```

## Live updates

For a stream (a sensor, a ticker) the series values are an `ObservableCollection<T>` owned by the page or a service; the chart animates each change.

```csharp
public ObservableCollection<double> Readings { get; } = [];

void OnReading(double value)
{
    Readings.Add(value);
    if (Readings.Count > 60) Readings.RemoveAt(0);
}
```

Add from the UI thread. Data that changes on a filter or refresh rebuilds the chart record from the feed instead.

## Your record as a point

Register a mapper once in `App.OnLaunched`, then use the record as `T`:

```csharp
LiveCharts.Configure(config => config.HasMap<MonthlyTotal>((total, index) => new(index, total.Amount)));
```

## Checks

- Each chart shows the expected number of bars, slices or points in the running app, with readable axis labels.
- In a card, the chart fills the card's width (`layout.md`).
- Labels and series stay readable after switching to dark mode (`theme-colors.md`).
