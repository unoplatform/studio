# Packages, platforms and installation

Pick from this table. Each package ships a Uno build; the WinUI-only packages next to them (`XAML.MapControl.WinUI`, `Esri.ArcGISRuntime.WinUI`, `CommunityToolkit.WinUI.UI.Controls.DataGrid` alone) build only for the Windows head.

| Need | Package (version checked) | XAML namespace | Targets in the package | Uno.Sdk 6.x | Uno.Sdk 7.0 previews |
|------|---------------------------|----------------|------------------------|-------------|----------------------|
| Charts bound to data | `LiveChartsCore.SkiaSharpView.Uno.WinUI` 2.0.5 | `using:LiveChartsCore.SkiaSharpView.WinUI` | desktop, browserwasm, android, ios, windows | works | `UNOB0023` (SkiaSharp.Views, Lottie) |
| Scientific plots, large signals | `ScottPlot.WinUI` 5.1.59 | `using:ScottPlot.WinUI` | net8/net10 (Skia heads), android, ios, maccatalyst, windows | works | `UNOB0023` (SkiaSharp.Views) |
| Maps | `Mapsui.Uno.WinUI` 5.1.0 | `using:Mapsui.UI.WinUI` | net9 (Skia heads), android, ios, maccatalyst, windows | works | `UNOB0023` (SkiaSharp.Views) |
| Data grid (default) | `WinUI.TableView` 1.5.1 | `using:WinUI.TableView` | net8/9/10 (all Uno heads), windows | works | `CS0012`, needs the `Uno` assembly |
| Data grid (Community Toolkit 7) | `Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid` 7.1.206 | `using:CommunityToolkit.WinUI.UI.Controls` | Uno heads; Windows head uses `CommunityToolkit.WinUI.UI.Controls.DataGrid` 7.1.2 | works | 7.2.0-dev builds target 7.0 |

All five were built and run on the desktop head with Uno.Sdk 6.7.30, bound to an MVUX `IListFeed`, in light and dark mode. The other heads come from each package's target frameworks; confirm them in the running app before reporting them as verified.

## Uno.Sdk 7.0

Read `msbuild-sdks` → `Uno.Sdk` in `global.json`. On a `7.0.0-dev.*` SDK:

- The build stops with `UNOB0023 ... did not provide any runtime assembly` for LiveCharts2 and Mapsui (and ScottPlot, which depends on the same package): their `SkiaSharp.Views.Uno.WinUI` dependency still packs the Uno 6 `uno-runtime/<tfm>/skia` layout.
- WinUI.TableView 1.5.1 stops with `CS0012: The type 'CoreDispatcher' is defined in an assembly that is not referenced ... 'Uno'`.
- Do not work around either error. Tell the user the control has no Uno 7 build yet, and offer Uno.Sdk 6.x for that app, or a different control.

When the authors publish 7.0 builds, the error goes away with a version bump; check NuGet for a newer version before reporting the control as unavailable.

## Install

The templates use central package management: the version goes in `Directory.Packages.props`, the reference in the app's `.csproj`. A `PackageReference` with a `Version` attribute fails with `NU1008`.

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" Version="2.0.5" />
  <PackageVersion Include="Mapsui.Uno.WinUI" Version="5.1.0" />
  <PackageVersion Include="WinUI.TableView" Version="1.5.1" />
</ItemGroup>

<!-- MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="LiveChartsCore.SkiaSharpView.Uno.WinUI" />
  <PackageReference Include="Mapsui.Uno.WinUI" />
  <PackageReference Include="WinUI.TableView" />
</ItemGroup>
```

These are plain NuGet packages: there is no `UnoFeatures` entry for them.

### The Community Toolkit DataGrid needs two packages

The Uno port covers every head except Windows; the Windows head (`net*-windows10.*`) takes Microsoft's package. Both use the `CommunityToolkit.WinUI.UI.Controls` namespace, so the XAML is shared.

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
  <PackageReference Include="CommunityToolkit.WinUI.UI.Controls.DataGrid" />
</ItemGroup>
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
  <PackageReference Include="Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid" />
</ItemGroup>
```

Add a `PackageVersion` for each (7.1.2 and 7.1.206). Without a Windows head, the second reference alone is enough. The other packages in the table ship a Windows build in the same package.

### WebAssembly

A Skia WebAssembly app relinks `libSkiaSharp.a` and `libHarfBuzzSharp.a`, which needs the `wasm-tools` workload whether or not these packages are present. `UNOWA0001 ... the wasm-tools workload could not be located` is a machine setup error: run `uno-check` or `dotnet workload install wasm-tools`; do not remove the package.

## Docs

The Uno docs list the supported libraries but carry no API pages for them; the package READMEs and the samples named in each reference are the source.

```
uno_platform_docs_search("supported third-party libraries charts")
uno_platform_docs_search("Windows Community Toolkit 7 DataGrid Uno")
```

Pages: `supported-libraries.md`, `uno-community-toolkit-v7.md`.
