# Size a chart

LiveCharts2 and ScottPlot charts have no intrinsic size: they take what their container gives them. Most "the chart doesn't show" reports are a chart with no height or no width.

- **Height:** a fixed or `*` row (`<RowDefinition Height="220" />`), or `Height` on the chart. An `Auto` row or a vertical `StackPanel` gives it none.
- **Width:** the chart stretches only when every container above it stretches. A card, a `Border` or a `StackPanel` aligned left or centered sizes to its widest text, and the chart gets that width.

## In a card

A chart has no intrinsic size. In an `Auto` row it collapses to nothing; in a control that sizes to its content it takes the width of its sibling text, about 100 pixels.

```xml
<utu:CardContentControl Style="{StaticResource FilledCardContentControlStyle}"
                        HorizontalAlignment="Stretch"
                        HorizontalContentAlignment="Stretch">
  <Grid Padding="16" RowSpacing="8">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition Height="220" />
    </Grid.RowDefinitions>
    <TextBlock Text="Spend by month" Style="{StaticResource TitleMedium}" />
    <lvc:CartesianChart Grid.Row="1" Series="{Binding Data.Series}" XAxes="{Binding Data.XAxes}" />
  </Grid>
</utu:CardContentControl>
```

The two `Stretch` attributes on the card are the fix; a `StackPanel` inside a stretched card works as well as a `Grid`. A sparkline in a KPI tile is a `CartesianChart` with `Height="48"`, `LegendPosition="Hidden"`, axes hidden (`new Axis { IsVisible = false }` in both `XAxes` and `YAxes`) and `GeometrySize = 0` on the `LineSeries`.

## Dashboard grid

Charts side by side share a `Grid` row with `*` columns, each column with a `MinWidth`, so a narrow window wraps them instead of squeezing them (the `uno-toolkit` skill, `references/responsive.md`, for the switch to one column).

## Check

In the running app, take each chart's bounds (`uno_app_find_elements` with `detail: "full"`): its width is the card's width minus padding, and its height is the row's.
