# Chart colors from the theme

LiveCharts2 and ScottPlot paint with SkiaSharp colors, which `{ThemeResource}` never reaches: by default their dark labels vanish on a dark background. Apply the theme's colors in code on load, on new series, and on a theme change.

**Contents:** [Read a color for the current theme](#read-a-color-for-the-current-theme) · [Series colors: a chart palette in App.xaml](#series-colors-a-chart-palette-in-appxaml) · [Apply it to LiveCharts2: one attached property](#apply-it-to-livecharts2-one-attached-property) · [Check](#check)

## Read a color for the current theme

`Application.Current.Resources["PrimaryColor"]` is wrong: after the app switches to dark mode it still answers with the light value. Look the key up in the theme dictionaries for the element's `ActualTheme`. WinUI keys the dark dictionary `"Default"`.

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

Text and lines come from the Uno Themes keys (Material and Simple): `OnSurfaceColor` for labels, legend and data labels, `OutlineVariantColor` for grid lines, `SurfaceVariantColor` for tooltips, `SurfaceColor` for a plot background (the `uno-themes` skill, `references/semantic-colors-brushes.md`). An app without Uno Themes has none of these keys and the helper returns transparent.

## Series colors: a chart palette in App.xaml

The theme's accent keys are not a chart palette. On Simple, `PrimaryColor` is near-white in dark mode (near-black in light) and `SecondaryColor` and `TertiaryColor` are the same gray (`#949494` in dark), so a donut's slices can't be told apart. Declare three series colors next to the theme in `App.xaml`, with a light and a dark value for each:

```xml
<!-- App.xaml, in Application.Resources' ResourceDictionary, after MergedDictionaries -->
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">
    <Color x:Key="ChartSeries1Color">#2A78D6</Color>
    <Color x:Key="ChartSeries2Color">#EB6834</Color>
    <Color x:Key="ChartSeries3Color">#1BAF7A</Color>
  </ResourceDictionary>
  <ResourceDictionary x:Key="Default">
    <Color x:Key="ChartSeries1Color">#3987E5</Color>
    <Color x:Key="ChartSeries2Color">#D95926</Color>
    <Color x:Key="ChartSeries3Color">#199E70</Color>
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

Blue, orange and green stay distinct from each other and for the common color-vision deficiencies. Replace them with the brand's chart colors when it has some; add `ChartSeries4Color` and up (and to `SeriesKeys` below) for more series.

## Apply it to LiveCharts2: one attached property

A chart inside a `FeedView` template has no `x:Name` field on the page, so the theming hangs off the chart itself. Set `local:ChartTheme.FollowTheme="True"` on each `CartesianChart` and `PieChart`.

```csharp
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinUI;
using SkiaSharp;

public static class ChartTheme
{
    public static readonly DependencyProperty FollowThemeProperty = DependencyProperty.RegisterAttached(
        "FollowTheme", typeof(bool), typeof(ChartTheme), new PropertyMetadata(false, OnFollowThemeChanged));
    public static bool GetFollowTheme(DependencyObject d) => (bool)d.GetValue(FollowThemeProperty);
    public static void SetFollowTheme(DependencyObject d, bool value) => d.SetValue(FollowThemeProperty, value);

    static readonly string[] SeriesKeys = ["ChartSeries1Color", "ChartSeries2Color", "ChartSeries3Color"];

    static void OnFollowThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || d is not FrameworkElement chart) return;
        chart.Loaded += (_, _) => Apply(chart);
        chart.ActualThemeChanged += (_, _) => Apply(chart);
        // A new feed value brings new series and axes with the default palette.
        if (chart is CartesianChart c)
        {
            c.RegisterPropertyChangedCallback(CartesianChart.SeriesProperty, (_, _) => Apply(chart));
            c.RegisterPropertyChangedCallback(CartesianChart.XAxesProperty, (_, _) => Apply(chart));
            c.RegisterPropertyChangedCallback(CartesianChart.YAxesProperty, (_, _) => Apply(chart));
        }
        else if (chart is PieChart p)
        {
            p.RegisterPropertyChangedCallback(PieChart.SeriesProperty, (_, _) => Apply(chart));
        }
    }

    static void Apply(FrameworkElement chart)
    {
        var text = Paint(chart, "OnSurfaceColor");
        var tooltip = Paint(chart, "SurfaceVariantColor");
        IEnumerable<ISeries> series;
        if (chart is CartesianChart c)
        {
            // Series and axes are null until the binding delivers them.
            foreach (var axis in (c.XAxes ?? []).Concat(c.YAxes ?? []).OfType<Axis>())
            {
                axis.LabelsPaint = text;
                axis.SeparatorsPaint = Paint(chart, "OutlineVariantColor");
            }
            (c.LegendTextPaint, c.TooltipTextPaint, c.TooltipBackgroundPaint) = (text, text, tooltip);
            series = c.Series ?? [];
        }
        else if (chart is PieChart p)
        {
            (p.LegendTextPaint, p.TooltipTextPaint, p.TooltipBackgroundPaint) = (text, text, tooltip);
            series = p.Series ?? [];
        }
        else return;

        var i = 0;
        foreach (var s in series)
        {
            var key = SeriesKeys[i++ % SeriesKeys.Length];
            var paint = Paint(chart, key);
            switch (s)
            {
                // Area under the line: the series color, mostly transparent. A plain line sets Fill = null in the Model.
                case LineSeries<double> line: (line.Stroke, line.GeometryStroke) = (paint, paint); if (line.Fill is not null) line.Fill = Paint(chart, key, 40); break;
                case LineSeries<DateTimePoint> dated: (dated.Stroke, dated.GeometryStroke) = (paint, paint); if (dated.Fill is not null) dated.Fill = Paint(chart, key, 40); break;
                case ColumnSeries<double> column: column.Fill = paint; if (column.DataLabelsPaint is SolidColorPaint) column.DataLabelsPaint = text; break;
                case RowSeries<double> row: row.Fill = paint; if (row.DataLabelsPaint is SolidColorPaint) row.DataLabelsPaint = text; break;
                case PieSeries<double> slice: slice.Fill = paint; break;
            }
        }
    }

    static SolidColorPaint Paint(FrameworkElement element, string key, byte? alpha = null)
    {
        var c = ThemeColors.Get(element, key);
        return new SolidColorPaint(new SKColor(c.R, c.G, c.B, alpha ?? c.A));
    }
}
```

- Add a `case` per other series type the app uses (`LineSeries<int>`, `StackedColumnSeries<double>`).
- Data labels are off until the Model sets `DataLabelsPaint = new SolidColorPaint()` on the series (`axes.md`). Repaint them only when they are a `SolidColorPaint`, as above: a series with labels off holds a placeholder paint, and assigning any paint turns its labels on.
- Bind `YAxes` from the chart record too (`[new Axis { MinLimit = 0 }]`): with none bound, the chart's own default axis keeps its dark labels.
- Keep colors out of the Model.

## Check

Switch the running app to dark mode (`SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme.Dark)` from Uno Toolkit, or the OS setting): axis labels, legend and series stay readable.
