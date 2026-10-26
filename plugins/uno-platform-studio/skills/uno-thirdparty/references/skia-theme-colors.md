# Theme colors for SkiaSharp-drawn controls

LiveCharts2, ScottPlot and Mapsui paint with SkiaSharp colors, not XAML brushes. `{ThemeResource}` does not reach them, so out of the box they draw their own palette (blue series, black text) and stay light in dark mode. Read the Uno Themes colors in code and apply them when the control loads, when its data changes, and when the theme changes.

## Read a color for the current theme

`Application.Current.Resources["PrimaryColor"]` is wrong: after the app switches to dark mode it still returns the light value. Look the key up in the theme dictionaries for the element's `ActualTheme`. WinUI keys the dark dictionary `"Default"`.

```csharp
static class ThemeColors
{
    public static Windows.UI.Color Get(FrameworkElement element, string key)
    {
        string[] themes = element.ActualTheme == ElementTheme.Dark ? ["Dark", "Default"] : ["Light"];
        return themes.Select(t => Find(Application.Current.Resources, t, key)).OfType<Windows.UI.Color>().FirstOrDefault();
    }

    static object? Find(ResourceDictionary dictionary, string theme, string key)
    {
        if (dictionary.ThemeDictionaries.TryGetValue(theme, out var t) && t is ResourceDictionary td && td.TryGetValue(key, out var v))
        {
            return v;
        }
        for (var i = dictionary.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (Find(dictionary.MergedDictionaries[i], theme, key) is { } found)
            {
                return found;
            }
        }
        return null;
    }
}
```

The keys are the Uno Themes color keys shared by Material and Simple: `PrimaryColor` for series, `OnSurfaceColor` for labels and axes, `SurfaceColor` for plot backgrounds, `OutlineVariantColor` for grid lines (the `uno-themes` skill, `references/semantic-colors-brushes.md`). On an app without Uno Themes these keys do not exist and the helper returns transparent.

## Apply it

```csharp
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

public SalesPage()
{
    this.InitializeComponent();
    Loaded += (_, _) => ApplyChartTheme();
    ActualThemeChanged += (_, _) => ApplyChartTheme();
    // A new feed value brings new series objects with the default palette.
    UnitsChart.RegisterPropertyChangedCallback(LiveChartsCore.SkiaSharpView.WinUI.CartesianChart.SeriesProperty, (_, _) => ApplyChartTheme());
}

void ApplyChartTheme()
{
    var text = ThemeColors.Get(this, "OnSurfaceColor");
    var primary = ThemeColors.Get(this, "PrimaryColor");
    var surface = ThemeColors.Get(this, "SurfaceColor");

    // LiveCharts2: Series and axes are null until the binding delivers them.
    var label = new SolidColorPaint(new SKColor(text.R, text.G, text.B));
    foreach (var axis in (UnitsChart.XAxes ?? []).Concat(UnitsChart.YAxes ?? []).OfType<Axis>()) axis.LabelsPaint = label;
    foreach (var series in (UnitsChart.Series ?? []).OfType<ColumnSeries<int>>()) series.Fill = new SolidColorPaint(new SKColor(primary.R, primary.G, primary.B));
    UnitsChart.LegendTextPaint = label;

    // ScottPlot
    TrendPlot.Plot.FigureBackground.Color = new ScottPlot.Color(surface.R, surface.G, surface.B);
    TrendPlot.Plot.DataBackground.Color = new ScottPlot.Color(surface.R, surface.G, surface.B);
    TrendPlot.Plot.Axes.Color(new ScottPlot.Color(text.R, text.G, text.B));
    TrendPlot.Refresh();
}
```

- **Null-check the chart's `Series`, `XAxes` and `YAxes`.** `Loaded` runs before an MVUX feed delivers them; an exception there, inside an `async` handler, is swallowed and stops the rest of the handler.
- Mapsui: build pin styles with `Mapsui.Styles.Color.FromArgb(c.A, c.R, c.G, c.B)` from the same helper, and rebuild the layer `Style` in `ActualThemeChanged`, then call `DataHasChanged()`. The OpenStreetMap tiles stay light.
- Keep colors out of the Model. The Model returns data; the page decides how it looks.

## Check

Switch the running app to dark mode (`SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme.Dark)` from Uno Toolkit, or the OS setting) and confirm the axis labels and series stay readable.
