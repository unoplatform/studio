# Install a chart package

## Pick

| Need | Package (version checked) | XAML namespace | Uno.Sdk 6.x | Uno.Sdk 7.x |
|------|---------------------------|----------------|-------------|-------------|
| Business charts bound to data: line, column, area, pie, donut, gauge, scatter, stacked, financial | `LiveChartsCore.SkiaSharpView.Uno.WinUI` 2.0.5 | `using:LiveChartsCore.SkiaSharpView.WinUI` | builds | `UNOB0023` |
| Plots of thousands of points (signals, sensor logs, scientific data), filled from code | `ScottPlot.WinUI` 5.1.59 | `using:ScottPlot.WinUI` | builds | `UNOB0023` |

Default to LiveCharts2: it binds, animates, and has tooltips and legends. Pick ScottPlot only when the point count makes LiveCharts2 slow (tens of thousands) or the plot is scientific.

Both target the desktop, WebAssembly, Android and iOS heads and ship a Windows build in the same package. Both were built on the desktop head with Uno.Sdk 6.7.49; confirm each other head in the running app before calling it verified.

Wrong packages that look right:

- `LiveChartsCore.SkiaSharpView.WinUI`: WinAppSDK only, no Uno heads.
- `LiveChartsCore.SkiaSharpView.Uno`: the old UWP-based Uno build, stuck at 2.0.0-rc2.
- `LiveChartsCore.SkiaSharpView`: the drawing engine without any control.
- `H.OxyPlot.Uno`, `H.Syncfusion.SfChart.Uno.WinUI`: community ports, years without a release.

## Uno.Sdk 7.x

Read `msbuild-sdks` → `Uno.Sdk` in `global.json`. On 7.x (checked on 7.0.22 and 7.1.0-dev.23), the build stops with:

```
error UNOB0023: The runtime-enabled package(s) [Uno.WinUI.Lottie;SkiaSharp.Views.Uno.WinUI] did not provide any runtime assembly for net10.0-desktop.
```

LiveCharts2 2.0.5 (and its 2.1 previews) depend on `SkiaSharp.Views.Uno.WinUI` 3.119 and `Uno.WinUI.Lottie` 6.3, which pack the Uno 6 layout; ScottPlot depends on the same `SkiaSharp.Views.Uno.WinUI`.

- Check NuGet for a newer LiveCharts2 or ScottPlot whose dependencies name Uno 7 packages. A version bump is the whole fix once one exists.
- Otherwise stop and tell the user before writing chart code: the chart packages have no Uno 7 build yet. The choices are theirs: pin this app's `Uno.Sdk` to the latest 6.7 release in `global.json` (and lose any 7.x feature it uses), or leave the charts out until a 7.0 build ships.
- Never get past it with `UnoDisableUNOB0023Validation`, a `PackageVersion` that raises `SkiaSharp.Views.Uno.WinUI` to 4.x, or `ExcludeAssets="all"` on `Uno.WinUI.Lottie`. Each one builds, and the app then runs a control compiled against Uno 6.

## Add it

Add the package and let it restore before any file names its types. Chart XAML or C# written first fails with `CS0246: 'ISeries' could not be found` and `CS0400: 'LiveChartsCore' could not be found in the global namespace`.

The templates use central package management: when `Directory.Packages.props` exists at or above the `.csproj`, the version goes there and the reference has none. A `Version` on the `PackageReference` fails with `NU1008`.

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" Version="2.0.5" />
</ItemGroup>

<!-- MyApp/MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" />
</ItemGroup>
```

Without `Directory.Packages.props`, put `Version="2.0.5"` on the `PackageReference`. Always pin a version. These are plain NuGet packages: there is no `UnoFeatures` entry for them, and no `Condition` on the reference, since every head uses the same package.

No startup code is needed. `LiveCharts.Configure(...)` in `App.xaml.cs` is only for a mapper from your own record type to a point (`references/livecharts.md`), non-Latin fonts, or right-to-left.

## WebAssembly

A Skia WebAssembly app relinks `libSkiaSharp.a`, which needs the `wasm-tools` workload, chart or no chart. `UNOWA0001 ... the wasm-tools workload could not be located` is a machine setup error: run `uno-check` or `dotnet workload install wasm-tools`; do not remove the package.

## Docs

The Uno docs list supported libraries but carry no API pages for these packages; the package READMEs and samples are the source.

```
uno_platform_docs_search("supported third-party libraries charts")
```

LiveCharts2: the `beto-rodriguez/LiveCharts2` repository, `samples/` (the Uno WinUI sample) and livecharts.dev. ScottPlot: scottplot.net, the WinUI quickstart.
