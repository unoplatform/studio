# ScottPlot

`ScottPlot.WinUI`, XAML prefix `xmlns:scottplot="using:ScottPlot.WinUI"`. Install it first (`install.md`). Use it for plots too dense for LiveCharts2: signals, sensor logs, scientific scatter plots with tens of thousands of points.

ScottPlot has no bindable data. The page fills the plot from code-behind and calls `Refresh()` after every change.

```xml
<Page xmlns:scottplot="using:ScottPlot.WinUI" ...>
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <scottplot:WinUIPlot x:Name="TrendPlot" AutomationProperties.AutomationId="TrendPlot" />
  </Grid>
</Page>
```

```csharp
TrendPlot.Plot.Add.Signal(samples);             // double[] at a fixed sample rate
// TrendPlot.Plot.Add.Scatter(xs, ys);          // paired double[]
// TrendPlot.Plot.Add.Bars(values);             // double[]
TrendPlot.Plot.Axes.Title.Label.Text = "Load";
TrendPlot.Plot.Axes.AutoScale();
TrendPlot.Refresh();
```

## With MVUX

The Model exposes the data as a feed; the page reads it when the `DataContext` delivers it. Keep plotting in the page, not in the Model.

```csharp
public sealed partial class SensorPage : Page
{
    public SensorPage()
    {
        InitializeComponent();
        DataContextChanged += async (_, _) =>
        {
            if (DataContext is SensorViewModel vm && await vm.Samples is { } samples)
            {
                TrendPlot.Plot.Clear();
                TrendPlot.Plot.Add.Signal(samples.ToArray());
                TrendPlot.Plot.Axes.AutoScale();
                TrendPlot.Refresh();
            }
        };
    }
}
```

`await feed` returns the feed's current value. When the data changes often, prefer LiveCharts2, which binds.

## Theme

ScottPlot paints a white figure with black text by default. Set its colors from the theme and refresh (`theme-colors.md`):

```csharp
TrendPlot.Plot.FigureBackground.Color = ToScottPlot(ThemeColors.Get(this, "SurfaceColor"));
TrendPlot.Plot.DataBackground.Color = ToScottPlot(ThemeColors.Get(this, "SurfaceColor"));
TrendPlot.Plot.Axes.Color(ToScottPlot(ThemeColors.Get(this, "OnSurfaceColor")));
TrendPlot.Plot.Grid.MajorLineColor = ToScottPlot(ThemeColors.Get(this, "OutlineVariantColor"));
TrendPlot.Refresh();

static ScottPlot.Color ToScottPlot(Windows.UI.Color c) => new(c.R, c.G, c.B, c.A);
```

## Checks

- The plot shows the data in the running app; an unrefreshed plot shows empty axes.
- Axes and background follow a switch to dark mode.
