# Chart colors from the theme

LiveCharts2 and ScottPlot paint with SkiaSharp colors, which `{ThemeResource}` never reaches: by default their dark labels vanish on a dark background. Apply the theme's colors in code on load, on new series, and on a theme change.

**Contents:** [Read a color for the current theme](#read-a-color-for-the-current-theme) · [Apply it to LiveCharts2: one attached property](#apply-it-to-livecharts2-one-attached-property) · [Check](#check)

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

The keys are the Uno Themes keys (Material and Simple): `PrimaryColor`, `TertiaryColor` and `SecondaryColor` for series, `OnSurfaceColor` for labels and legend, `OutlineVariantColor` for grid lines, `SurfaceVariantColor` for tooltips, `SurfaceColor` for a plot background (the `uno-themes` skill, `references/semantic-colors-brushes.md`). An app without Uno Themes has none of these keys and the helper returns transparent.

## Apply it to LiveCharts2: one attached property

A chart inside a `FeedView` template has no `x:Name` field on the page, so the theming hangs off the chart itself. Set `local:ChartTheme.FollowTheme="True"` on each `CartesianChart` and `PieChart`.

```csharp
using LiveChartsCore;
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

    static readonly string[] SeriesKeys = ["PrimaryColor", "TertiaryColor", "SecondaryColor"];

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
            var paint = Paint(chart, SeriesKeys[i++ % SeriesKeys.Length]);
            switch (s)
            {
                case LineSeries<double> line: line.Stroke = paint; line.GeometryStroke = paint; break;
                case ColumnSeries<double> column: column.Fill = paint; break;
                case PieSeries<double> slice: slice.Fill = paint; break;
            }
        }
    }

    static SolidColorPaint Paint(FrameworkElement element, string key)
    {
        var c = ThemeColors.Get(element, key);
        return new SolidColorPaint(new SKColor(c.R, c.G, c.B, c.A));
    }
}
```

- Add a `case` per other series type the app uses (`RowSeries<T>`, `LineSeries<int>`).
- Bind `YAxes` from the chart record too (`[new Axis { MinLimit = 0 }]`): with none bound, the chart's own default axis keeps its dark labels.
- Keep colors out of the Model.

## Check

Switch the running app to dark mode (`SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme.Dark)` from Uno Toolkit, or the OS setting): axis labels, legend and series stay readable.
