---
name: uno-charts
description: "Charts for Uno Platform apps with third-party packages that support Uno: LiveCharts2 (LiveChartsCore.SkiaSharpView.Uno.WinUI: CartesianChart, PieChart, PolarChart, line, column, area, pie, donut, gauge, scatter, stacked, financial series) and ScottPlot (ScottPlot.WinUI) for large signals. Covers picking the package, the Uno.Sdk versions each runs on, installation with central package management, binding series to MVUX feeds, sizing a chart in a card, axes and labels, live updates, and colors from the Uno Themes palette in light and dark mode. Use whenever an Uno app shows numbers as a picture: a dashboard, KPI tile, trend, sparkline, report, analytics or metrics page, sales by month, spend by category, a histogram, even when the user only says 'show how it changed over time' or 'visualize the data'. Read it before adding a chart package, and before drawing a chart by hand with Rectangle, Path, Canvas or a Grid of bars."
metadata:
  author: uno-platform
  category: charts
---

# Uno charts

Uno Platform ships no chart control. Without guidance agents pick a WinUI-only package that never restores for the Uno heads, draw bars out of `Rectangle`s, or bind the chart in a way that compiles and renders nothing. This skill names the packages that run on Uno, how to install them, and the binding, sizing and theming traps they share.

## Workflow

1. Read `global.json` for the `Uno.Sdk` version, then `references/install.md`. On Uno.Sdk 7.x LiveCharts2 and ScottPlot do not build yet: tell the user before writing any chart code.
2. Add the package and restore before writing XAML or C# that names its types; otherwise every chart type fails with `CS0246`.
3. Read `references/livecharts.md` for business charts bound to data, or `references/scottplot.md` for plots of thousands of points filled from code.
4. Size it with `references/layout.md`, and paint it from the theme with `references/theme-colors.md`: the charts draw with SkiaSharp and ignore `{ThemeResource}`.
5. Run the app and look at every chart (the `uno-testing` skill). An empty chart, a chart 100 pixels wide and black labels on a dark background all compile cleanly.

## Topic map

| Task / need | Read | Package and key API |
|-------------|------|---------------------|
| Which package, which Uno.Sdk and platforms, install with `Directory.Packages.props`, the wrong look-alike packages, WebAssembly | `references/install.md` | `PackageVersion`, `PackageReference` |
| Line, column, area, pie, donut, gauge, scatter, stacked chart bound to an MVUX feed; legend, tooltips, live updates | `references/livecharts.md` | `lvc:CartesianChart`, `lvc:PieChart`, `ISeries`, `Axis`, `ColumnSeries<T>` |
| Category labels, every label shown, value formats, a date axis for daily or weekly data, values printed on bars | `references/axes.md` | `Axis.Labels`, `DateTimePoint`, `Labeler`, `MinStep`, `DataLabelsPaint` |
| A chart that is empty, a sliver, or 100 pixels wide; a chart in a card, KPI tile or dashboard grid | `references/layout.md` | `RowDefinition`, `HorizontalContentAlignment`, `CardContentControl` |
| Signal, scientific or scatter plot with thousands of points, drawn from code | `references/scottplot.md` | `scottplot:WinUIPlot`, `Plot.Add.Signal`, `Refresh()` |
| Series colors from a chart palette in `App.xaml`, axis and label colors from the theme, both following a light/dark switch | `references/theme-colors.md` | `ThemeDictionaries`, `ActualThemeChanged`, `SolidColorPaint` |

## Critical rules

- **Use a chart package, never a hand-drawn chart.** Bars made of `Rectangle`s, a line of `Path` segments or an `ItemsRepeater` of scaled `Border`s have no axes, scaling, tooltips or accessibility, and break on the next data shape.
- **Check the Uno.Sdk major before installing.** LiveCharts2 2.0.5 and ScottPlot 5.1.59 build on Uno.Sdk 6.x. On 7.x both stop with `UNOB0023`: they still depend on Uno 6 builds of `SkiaSharp.Views.Uno.WinUI` and `Uno.WinUI.Lottie`. Never set `UnoDisableUNOB0023Validation`, override `SkiaSharp.Views.Uno.WinUI` or exclude `Uno.WinUI.Lottie` to get past it: the app then runs a binary built for Uno 6. Tell the user and let them choose between Uno.Sdk 6.7 for this app and waiting for a 7.0 build (`references/install.md`).
- **Install `LiveChartsCore.SkiaSharpView.Uno.WinUI`.** `LiveChartsCore.SkiaSharpView.WinUI` is WinAppSDK only, `LiveChartsCore.SkiaSharpView.Uno` is the old UWP build, and `LiveChartsCore.SkiaSharpView` has no controls. The XAML namespace is `using:LiveChartsCore.SkiaSharpView.WinUI`, without `.Uno`.
- **Never bind `Series` to an `IFeed<ISeries[]>` or `IState<ISeries[]>`.** The chart stays empty with no binding error. Put the series and axes in a record (`IFeed<SalesChart>`) and bind `{Binding Chart.Series}`.
- **Every file that names a chart type imports it.** `using LiveChartsCore;` and `using LiveChartsCore.SkiaSharpView;` are not in the template's global usings.
- **A chart needs a definite width and height.** It has no intrinsic size: in an `Auto` row or an unstretched card it collapses. Give it a fixed or `*` row, and stretch a `CardContentControl` that holds one (`HorizontalAlignment` and `HorizontalContentAlignment` both `Stretch`; `references/layout.md`).
- **Series values are numbers; category names go on the axis.** `ColumnSeries<double>` with `Axis.Labels`, never strings as values. Without labels the X axis reads 0, 1, 2. Daily or weekly data is `DateTimePoint` on a date axis with a guarded `Labeler`, never 90 strings in `Labels` (`references/axes.md`).
- **Series colors come from a chart palette in `App.xaml`, not the theme's accents.** On Simple, `PrimaryColor` is near-white in dark mode and `SecondaryColor` and `TertiaryColor` are the same gray: slices and series become indistinguishable (`references/theme-colors.md`).
- **The Model shapes data, the page paints it.** Totals per month or per category come from the service (a grouped query when the API can aggregate); the chart record holds the numbers and labels; colors are applied on the page from the theme.
- **Give each chart an `AutomationProperties.AutomationId`**, so a test can find it and screenshot it.

## Related skills

- `uno-mvux` for the feeds the chart record is built from (`references/feed-basics.md`, `references/listfeed.md`).
- `uno-toolkit` for the card or tile a chart sits in (`references/card.md`).
- `uno-themes` for the color keys a chart is painted with (`references/semantic-colors-brushes.md`).
- `uno-kiota` when the numbers come from a web API, and the totals should come from it too.
- `uno-testing` to confirm each chart rendered its data, in light and dark.
