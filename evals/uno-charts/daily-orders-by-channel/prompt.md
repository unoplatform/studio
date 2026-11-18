---
description: 90 daily points go on a date axis with a Labeler that cannot throw, not 90 strings in Axis.Labels, and the donut's slices take a chart palette, not Simple's gray accent colors.
tags: [uno-charts, livecharts]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app is on the recommended template (MVUX, Simple theme, Uno.Sdk 6.7.49, Directory.Packages.props at the root). The Insights page Model already has these:

```csharp
public partial record InsightsModel(IOrderService Orders)
{
    public IListFeed<DailyCount> Daily => ListFeed.Async(ct => Orders.GetDailyAsync(days: 90, ct));
    public IListFeed<ChannelShare> Channels => ListFeed.Async(Orders.GetByChannelAsync);
}

public record DailyCount(DateOnly Date, int Orders);
public record ChannelShare(string Channel, int Orders); // Web, Mobile, Marketplace
```

I want a line chart of the 90 days of orders and a donut of the three channels underneath it, and both have to look right in dark mode. Show me the package setup, the Model change and the XAML.
