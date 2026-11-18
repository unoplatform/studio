# Install Mapsui

**Contents:** [Pick](#pick) · [Uno.Sdk 7.x](#unosdk-7x) · [Add it](#add-it) · [WebAssembly](#webassembly) · [Docs](#docs)

## Pick

| Package (version checked) | XAML namespace | Uno.Sdk 6.7 | Uno.Sdk 7.x |
|---------------------------|----------------|-------------|-------------|
| `Mapsui.Uno.WinUI` 5.1.0 | `using:Mapsui.UI.WinUI` | builds | `UNOB0023` |
| `Mapsui.Uno.WinUI` 5.2.0 | `using:Mapsui.UI.WinUI` | `NU1605` until `SkiaSharpVersion` is 3.119.4 | `UNOB0023` |

Default to 5.1.0 on Uno.Sdk 6.7: its SkiaSharp 3.119.2 is the one the SDK brings. It pulls in `Mapsui`, `Mapsui.Tiling` (OpenStreetMap and other tile sources), `Mapsui.Nts` (lines and areas) and BruTile; reference only `Mapsui.Uno.WinUI`.

The package targets the desktop, WebAssembly, Android and iOS heads and ships a Windows build. It was built on the desktop head with Uno.Sdk 6.7.49, next to LiveCharts2 and ScottPlot; confirm each other head in the running app before calling it verified.

Wrong packages that look right:

- `Uno.WinUI.Maps` (the `Maps` feature): Uno's `MapControl`, Android and iOS only, with platform map keys.
- `Mapsui.Uno`: the old UWP-based Uno build, stuck at 4.1.9.
- `XAML.MapControl.WinUI`, `Esri.ArcGISRuntime.WinUI`: WinAppSDK only, no Uno heads.
- `Mapsui`, `Mapsui.Tiling`: the engine without a control.

### `NU1605` with 5.2.0

```
error NU1605: Warning As Error: Detected package downgrade: SkiaSharp.Views.Uno.WinUI from 3.119.4 to 3.119.2.
```

Mapsui 5.2.0 asks for SkiaSharp 3.119.4 and Uno.Sdk 6.7.49 pins 3.119.2. Use 5.1.0, or raise the SDK's SkiaSharp patch with its own property in the app `.csproj`:

```xml
<PropertyGroup>
  <SkiaSharpVersion>3.119.4</SkiaSharpVersion>
</PropertyGroup>
```

A `PackageVersion` or `PackageReference` for `SkiaSharp.Views.Uno.WinUI` 3.119.4 does not help: the SDK's implicit references keep asking for 3.119.2 and restore fails the same way. A 3.119 patch is the only raise this property gets. 4.x is the Uno 7 SkiaSharp and does not fix anything on 6.7.

## Uno.Sdk 7.x

Read `msbuild-sdks` → `Uno.Sdk` in `global.json`. On 7.x (checked on 7.0.22 and 7.1.0-dev.23), the build stops with:

```
error UNOB0023: The runtime-enabled package(s) [SkiaSharp.Views.Uno.WinUI] did not provide any runtime assembly for net10.0-desktop.
```

Mapsui 5.x depends on `SkiaSharp.Views.Uno.WinUI` 3.119 and `Uno.WinUI` 6.5, which pack the Uno 6 layout.

- Check NuGet for a newer `Mapsui.Uno.WinUI` whose dependencies name Uno 7 packages. A version bump is the whole fix once one exists.
- Otherwise stop and tell the user before writing map code: Mapsui has no Uno 7 build yet. The choices are theirs: pin this app's `Uno.Sdk` to the latest 6.7 release in `global.json` (and lose any 7.x feature it uses), or leave the map out until a 7.0 build ships.
- Never get past it with `UnoDisableUNOB0023Validation` or a `SkiaSharpVersion`/`PackageVersion` that raises `SkiaSharp.Views.Uno.WinUI` to 4.x. Each one builds, and the app then runs a control compiled against Uno 6.

## Add it

Add the package and let it restore before any file names its types. Map XAML or C# written first fails with `CS0246: 'MapControl' could not be found` and `CS0400: 'Mapsui' could not be found in the global namespace`.

The templates use central package management: when `Directory.Packages.props` exists at or above the `.csproj`, the version goes there and the reference has none. A `Version` on the `PackageReference` fails with `NU1008`.

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="Mapsui.Uno.WinUI" Version="5.1.0" />
</ItemGroup>

<!-- MyApp/MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="Mapsui.Uno.WinUI" />
</ItemGroup>
```

Without `Directory.Packages.props`, put `Version="5.1.0"` on the `PackageReference`. Always pin a version. There is no `UnoFeatures` entry for Mapsui, and no `Condition` on the reference: every head uses the same package. No startup code is needed.

## WebAssembly

A Skia WebAssembly app relinks `libSkiaSharp.a`, which needs the `wasm-tools` workload. `UNOWA0001 ... the wasm-tools workload could not be located` is a machine setup error: run `uno-check` or `dotnet workload install wasm-tools`; do not remove the package. Tiles load from the browser, so the tile server must send CORS headers (`references/tiles-and-colors.md`).

## Docs

The Uno docs carry no API pages for Mapsui; the Mapsui docs and samples are the source.

```
uno_platform_docs_search("Chefs Mapsui map")
```

mapsui.com (the v5 documentation: layers, navigation, projections, tile providers) and the `Mapsui/Mapsui` repository, `Samples/Mapsui.Samples.Uno.WinUI`. The API changed between 4.x and 5.x: trust 5.x samples only.
