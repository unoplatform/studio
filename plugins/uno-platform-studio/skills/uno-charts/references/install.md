# Install a chart package

## Pick

| Need | Package (version checked) | XAML namespace | Uno.Sdk 6.x | Uno.Sdk 7.x |
|------|---------------------------|----------------|-------------|-------------|
| Business charts bound to data: line, column, area, pie, donut, gauge, scatter, stacked, financial | `LiveChartsCore.SkiaSharpView.Uno.WinUI` 2.0.5 on Uno.Sdk 6.x, 2.2.0 on 7.x | `using:LiveChartsCore.SkiaSharpView.WinUI` | 2.0.5 builds; 2.2.0 fails with `NU1605` | 2.2.0 builds; 2.0.5 fails with `UNOB0023` |
| Plots of thousands of points (signals, sensor logs, scientific data), filled from code | `ScottPlot.WinUI` 5.1.59 | `using:ScottPlot.WinUI` | builds | `UNOB0023` |

Default to LiveCharts2: it binds, animates, and has tooltips and legends. Pick ScottPlot only when the point count makes LiveCharts2 slow (tens of thousands) or the plot is scientific.

Both target the desktop, WebAssembly, Android and iOS heads and ship a Windows build in the same package. LiveCharts2 2.0.5 and ScottPlot were built on the desktop head with Uno.Sdk 6.7.49. LiveCharts2 2.2.0 was built on the desktop, WebAssembly and Android heads with Uno.Sdk 7.0.22 and rendered on desktop; it ships only `net10.0` and `net10.0-windows` builds, so the app must target .NET 10. Confirm each other head in the running app before calling it verified.

Wrong packages that look right:

- `LiveChartsCore.SkiaSharpView.WinUI`: WinAppSDK only, no Uno heads.
- `LiveChartsCore.SkiaSharpView.Uno`: the old UWP-based Uno build, stuck at 2.0.0-rc2.
- `LiveChartsCore.SkiaSharpView`: the drawing engine without any control.
- `H.OxyPlot.Uno`, `H.Syncfusion.SfChart.Uno.WinUI`: community ports, years without a release.

## Uno.Sdk 7.x

Read `msbuild-sdks` → `Uno.Sdk` in `global.json`. On 7.x, install LiveCharts2 **2.2.0** (needs Uno.Sdk 7.0.5 or later, for `Uno.WinUI` 7.0.32). It depends on Uno 7 packages (`Uno.WinUI.Composition.Skia`, `Uno.WinUI.Graphics2DSK`) and no longer on `SkiaSharp.Views.Uno.WinUI` or `Uno.WinUI.Lottie`. The XAML namespace and the code in `livecharts.md`, `axes.md` and `theme-colors.md` are unchanged.

The versions don't cross Uno majors:

- 2.0.5 (and the 2.1 previews) on Uno.Sdk 7.x stops with:

  ```
  error UNOB0023: The runtime-enabled package(s) [Uno.WinUI.Lottie;SkiaSharp.Views.Uno.WinUI] did not provide any runtime assembly for net10.0-desktop.
  ```

  They depend on `SkiaSharp.Views.Uno.WinUI` 3.119 and `Uno.WinUI.Lottie` 6.3, which pack the Uno 6 layout. The fix is the version bump to 2.2.0.
- 2.2.0 on Uno.Sdk 6.x fails restore with `NU1605: Detected package downgrade: Uno.WinUI from 7.0.32 to 6.7.424`. Stay on 2.0.5 there; never raise `Uno.WinUI` by hand.

ScottPlot 5.1.59 has no Uno 7 build yet: on 7.x it stops with the same `UNOB0023` on `SkiaSharp.Views.Uno.WinUI`.

- Check NuGet for a newer ScottPlot whose dependencies name Uno 7 packages. A version bump is the whole fix once one exists.
- Otherwise prefer LiveCharts2 2.2.0 when it can draw the plot. When only ScottPlot will do, stop and tell the user before writing chart code: the choices are theirs, pin this app's `Uno.Sdk` to the latest 6.7 release in `global.json` (and lose any 7.x feature it uses), or wait for a ScottPlot 7.0 build.
- Never get past `UNOB0023` with `UnoDisableUNOB0023Validation`, a `PackageVersion` that raises `SkiaSharp.Views.Uno.WinUI` to 4.x, or `ExcludeAssets="all"` on `Uno.WinUI.Lottie`. Each one builds, and the app then runs a control compiled against Uno 6.

## Add it

Add the package and let it restore before any file names its types. Chart XAML or C# written first fails with `CS0246: 'ISeries' could not be found` and `CS0400: 'LiveChartsCore' could not be found in the global namespace`.

The templates use central package management: when `Directory.Packages.props` exists at or above the `.csproj`, the version goes there and the reference has none. A `Version` on the `PackageReference` fails with `NU1008`.

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <!-- 2.2.0 on Uno.Sdk 7.x, 2.0.5 on 6.x -->
  <PackageVersion Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" Version="2.2.0" />
</ItemGroup>

<!-- MyApp/MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" />
</ItemGroup>
```

Without `Directory.Packages.props`, put the `Version` on the `PackageReference`. Always pin a version. These are plain NuGet packages: there is no `UnoFeatures` entry for them, and no `Condition` on the reference, since every head uses the same package.

No startup code is needed. `LiveCharts.Configure(...)` in `App.xaml.cs` is only for a mapper from your own record type to a point (`references/livecharts.md`), non-Latin fonts, or right-to-left.

## WebAssembly

A Skia WebAssembly app relinks `libSkiaSharp.a`, which needs the `wasm-tools` workload, chart or no chart. `UNOWA0001 ... the wasm-tools workload could not be located` is a machine setup error: run `uno-check` or `dotnet workload install wasm-tools`; do not remove the package.

## Docs

The Uno docs list supported libraries but carry no API pages for these packages; the package READMEs and samples are the source.

```
uno_platform_docs_search("supported third-party libraries charts")
```

LiveCharts2: the `beto-rodriguez/LiveCharts2` repository, `samples/` (the Uno WinUI sample) and livecharts.dev. ScottPlot: scottplot.net, the WinUI quickstart.
