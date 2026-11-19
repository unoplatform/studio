---
type: llm
---
The reply answers a developer on Uno.Sdk 7.0.22 who wants a chart.
Facts the judge needs: LiveCharts2 2.0.5 (`LiveChartsCore.SkiaSharpView.Uno.WinUI`) and ScottPlot.WinUI 5.1.59 depend on Uno 6 builds of `SkiaSharp.Views.Uno.WinUI` (and LiveCharts2 on `Uno.WinUI.Lottie`), so on Uno.Sdk 7.x the build stops with `UNOB0023`. Neither has a 7.0 build yet. The developer's options are pinning the app to Uno.Sdk 6.7 or waiting for a 7.0 build.
PASS if it tells the developer that the chart package does not build on Uno.Sdk 7 yet.
FAIL if it gives LiveCharts2 or ScottPlot setup as if it works on 7.0.22, or hides the problem behind a suppressed check or forced dependency versions.
