---
description: A chart on an MVUX page uses LiveCharts2's Uno package, keeps the series in a record fed by the data feed instead of an IFeed<ISeries[]>, and is not drawn by hand.
tags: [uno-charts, livecharts]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I have an Uno Platform app from the recommended template (MVUX, Simple theme, Uno.Sdk 6.7.49 in global.json, Directory.Packages.props at the root). The Insights page Model already has this:

```csharp
public partial record InsightsModel(IExpenseService Expenses)
{
    public IListFeed<MonthTotal> Months => ListFeed.Async(Expenses.GetLast12MonthsAsync);
}

public record MonthTotal(string Month, decimal Total);
```

I want a bar chart of the 12 monthly totals at the top of the page, inside a card, that looks right in dark mode too. Show me the package setup, the Model change and the XAML.
