---
type: llm
---
The reply answers a developer on Uno.Sdk 7.0.22 who wants a line chart.
Facts the judge needs: LiveCharts2 2.2.0 (`LiveChartsCore.SkiaSharpView.Uno.WinUI`) is the first version built for Uno 7: it depends on `Uno.WinUI` 7.0.32 or later and builds on Uno.Sdk 7.0.22. Version 2.0.5 and the 2.1 previews depend on Uno 6 builds of `SkiaSharp.Views.Uno.WinUI` and `Uno.WinUI.Lottie`, so on Uno.Sdk 7.x the build stops with `UNOB0023`. ScottPlot.WinUI 5.1.59 has no Uno 7 build.
PASS if it installs `LiveChartsCore.SkiaSharpView.Uno.WinUI` at version 2.2.0 (or a later 2.x).
FAIL if it pins 2.0.5 or a 2.1 preview, recommends ScottPlot for this chart, tells the developer charts don't build on Uno 7, or suppresses `UNOB0023` or forces dependency versions.
