---
type: llm
---
The reply diagnoses an MVUX app whose LiveCharts2 `CartesianChart` shows axes but no bars, with `Series` bound to an `IFeed<ISeries[]>` property of the Model.
Facts the judge needs: binding a chart's `Series` to an `IFeed<ISeries[]>` or `IState<ISeries[]>` leaves the chart empty with no binding error. Wrapping the series (and axes) in a record exposed as `IFeed<ChartRecord>` and binding `{Binding Chart.Series}` renders, as does a plain `ISeries[]` property.
PASS if it names binding `Series` to the `IFeed<ISeries[]>` as the cause.
FAIL if it blames the data, the chart's size alone, or a missing `LiveCharts.Configure` call, and keeps the `IFeed<ISeries[]>`.
