---
name: uno-maps
description: "Interactive maps for Uno Platform apps with Mapsui (Mapsui.Uno.WinUI: MapControl, tile layers, pins, routes, areas, the user's location), the map package that runs on every Uno head. Covers the Uno.Sdk versions it builds on, install with central package management, pins bound to MVUX feeds, taps that run a Model command, centering and framing, longitude/latitude projection, OpenStreetMap tile rules and attribution, keyed tile providers, and pin colors from the theme. Use whenever an Uno app shows places: a store or station locator, delivery or fleet tracking, a route planner, listings on a map, a location picker, even when the user only says 'show them on a map' or 'near me'. Also use to fix a Mapsui map: pins stacked at one spot or in the ocean, no tiles, an empty map, a tap that does nothing. Read it before adding a map package, and before using WinUI's MapControl, a WebView running Leaflet or Google Maps, or a list where the page should be a map."
metadata:
  author: uno-platform
  category: maps
---

# Uno maps

Uno Platform's own `MapControl` (the `Maps` feature, `Uno.WinUI.Maps`) runs only on Android and iOS. For a map on every head, the package is Mapsui. Without guidance agents pick the WinUI-only map packages, embed a JavaScript map in a `WebView`, or put raw latitude and longitude on the map so every pin lands in the ocean off West Africa. This skill names the package, how to install it, and how to feed it an MVUX Model's data.

## Workflow

1. Read `global.json` for the `Uno.Sdk` version, then `references/install.md`. On Uno.Sdk 7.x Mapsui does not build yet: tell the user before writing any map code.
2. Add the package and restore before writing XAML or C# that names its types; otherwise every map type fails with `CS0246`.
3. Bind the pins with `references/pins-from-feeds.md`: `MapControl` has no `ItemsSource`, so a small attached property turns a feed into a layer.
4. Center, zoom, draw routes and areas, show the user's location with `references/map-basics.md`.
5. Pick the tile source and colors with `references/tiles-and-colors.md`: the public OpenStreetMap servers are for light use only, and the map draws with SkiaSharp, so `{ThemeResource}` never reaches the pins.
6. Run the app and look at the map (the `uno-testing` skill): tiles loaded, pins where the places are, a tap selects the right one. A map with no tiles or with every pin at one spot compiles cleanly.

## Topic map

| Task / need | Read | Package and key API |
|-------------|------|---------------------|
| Which package, which Uno.Sdk and platforms, install with `Directory.Packages.props`, `NU1605` on SkiaSharp, the wrong look-alike packages | `references/install.md` | `PackageVersion`, `SkiaSharpVersion` |
| Pins from an MVUX feed, a tap that selects a pin, refresh, a map inside a `FeedView` | `references/pins-from-feeds.md` | `mapsui:MapControl`, `MemoryLayer`, `PointFeature`, `Map.Info` |
| Center and zoom, frame all pins, longitude/latitude to map coordinates and back, a route line, a shaded area, labels, the user's location | `references/map-basics.md` | `SphericalMercator`, `Navigator`, `GeometryFeature`, `MyLocationLayer` |
| OpenStreetMap use policy, user agent, attribution, a keyed tile provider, WebAssembly CORS, pin and route colors in light and dark | `references/tiles-and-colors.md` | `OpenStreetMap.CreateTileLayer`, `HttpTileSource`, `SymbolStyle` |

## Critical rules

- **Use Mapsui for a map, never a substitute.** Not `Microsoft.UI.Xaml.Controls.Maps.MapControl` (Android and iOS only on Uno, through `Uno.WinUI.Maps`), not a `WebView` running Leaflet, MapLibre or Google Maps JavaScript, not a static image with pins on it, and not a `ListView` on a page the user asked to be a map.
- **Check the Uno.Sdk major before installing.** Mapsui 5.x builds on Uno.Sdk 6.x. On 7.x it stops with `UNOB0023`: it still depends on the Uno 6 build of `SkiaSharp.Views.Uno.WinUI`. Never set `UnoDisableUNOB0023Validation` or raise `SkiaSharpVersion` to 4.x to get past it. Tell the user and let them choose between Uno.Sdk 6.7 for this app and waiting for a Mapsui build for Uno 7 (`references/install.md`).
- **Install `Mapsui.Uno.WinUI`.** `Mapsui.Uno` is the old UWP-based build (4.1.9), `XAML.MapControl.WinUI` and `Esri.ArcGISRuntime.WinUI` build only for the Windows head. The XAML namespace is `using:Mapsui.UI.WinUI`.
- **Project every coordinate.** `SphericalMercator.FromLonLat(longitude, latitude).ToMPoint()`: longitude first, and `ToMPoint()` comes from `using Mapsui.Extensions;`. A raw latitude/longitude pair used as map coordinates puts every pin a few meters from (0, 0).
- **Write the clashing names in full in C#.** `Mapsui.UI.WinUI.MapControl`, `Mapsui.Styles.Brush` and `Mapsui.Styles.Color` collide with WinUI types once `Microsoft.UI.Xaml.Media` or `Windows.UI` is in scope; use a `using` alias.
- **The Model shapes the pins, the page draws them.** The Model exposes a feed of pins (an id, a label, latitude, longitude), and a tap runs a Model command. Layers, styles and colors live on the page side.
- **Listen to `CollectionChanged`.** Inside a `FeedView`, `Data` is a collection view that starts empty and fills in place, so a map that reads it only when the binding sets it shows no pins.
- **A map needs a definite width and height.** In an `Auto` row or a `StackPanel` it collapses to nothing. Give it a `*` row or a fixed `Height`, and stretch the `FeedView` that holds it.
- **Show the tile attribution.** OpenStreetMap tiles require "© OpenStreetMap contributors" on the map; Mapsui draws a tile layer's attribution, so never hide it or cover it.
- **Give the map an `AutomationProperties.AutomationId`**, so a test can find it and screenshot it.

## Related skills

- `uno-mvux` for the feed the pins are built from and the command a tap runs (`references/feedview.md`).
- `uno-themes` for the color keys the pins are painted with (`references/semantic-colors-brushes.md`).
- `uno-kiota` when the places come from a web API.
- `uno-testing` to confirm the tiles, the pins and the tap in the running app.
