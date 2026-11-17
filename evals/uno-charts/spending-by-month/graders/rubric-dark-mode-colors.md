---
type: llm
---
The reply adds a LiveCharts2 bar chart to an Uno Platform page that uses the Simple theme and must look right in dark mode.
Facts the judge needs: LiveCharts2 draws with SkiaSharp and ignores `{ThemeResource}` and `Foreground`; its axis labels, legend and series take `SolidColorPaint` values set in code (`LabelsPaint`, `Fill`, `LegendTextPaint`). Its default label color is dark, unreadable on a dark background. Reading the color with `Application.Current.Resources[key]` keeps returning the startup theme's value after a switch.
PASS if the chart's paints follow a switch between light and dark: the code re-reads the theme's colors on each theme change (`ActualThemeChanged` or an equivalent).
FAIL if it relies on `{ThemeResource}` or `Foreground` on the chart for its labels, or sets the colors once at startup only.
