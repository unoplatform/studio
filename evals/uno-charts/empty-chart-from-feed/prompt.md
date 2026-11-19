---
description: A LiveCharts2 chart bound to an IFeed<ISeries[]> renders empty; the fix wraps the series in a record and binds through it.
tags: [uno-charts, livecharts]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform (MVUX) app has a LiveCharts chart that shows axes but never any bars. No binding errors in the output. The data service definitely returns 7 values.

```csharp
public partial record StatsModel(IStatsService Stats)
{
    public IFeed<ISeries[]> Series => Feed.Async(async ct =>
    {
        var days = await Stats.GetWeekAsync(ct);
        return new ISeries[] { new ColumnSeries<double> { Values = days.Select(d => d.Visits).ToArray() } };
    });
}
```

```xml
<lvc:CartesianChart Grid.Row="1" Series="{Binding Series}" />
```

What's wrong?
