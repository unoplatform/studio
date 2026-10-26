# Maps: Mapsui

`Mapsui.Uno.WinUI` draws tiled maps, pins and shapes with SkiaSharp on every Uno head. Its `MapControl` has no bindable data surface: the page builds the layers in code-behind and feeds them the Model's data. Uno Chefs (`Chefs/Views/MapPage.xaml.cs`) is a complete example.

```xml
<Page xmlns:mapsui="using:Mapsui.UI.WinUI" ...>
  <mapsui:MapControl x:Name="SitesMap" AutomationProperties.AutomationId="SitesMap" />
</Page>
```

```csharp
using Mapsui;
using Mapsui.Extensions;   // ToMPoint() on the (x, y) tuple
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;

public sealed partial class SitesPage : Page
{
    MemoryLayer? _pins;

    public SitesPage()
    {
        this.InitializeComponent();
        Loaded += (_, _) => _pins ??= CreateLayers(SitesMap);
        DataContextChanged += async (_, _) =>
        {
            // The generated ViewModel exposes the Model; await the feed's current value.
            if (DataContext is SitesViewModel vm && await vm.Model.Sites.Value(CancellationToken.None) is { } sites)
            {
                ShowSites(_pins ??= CreateLayers(SitesMap), sites);
            }
        };
    }

    static MemoryLayer CreateLayers(Mapsui.UI.WinUI.MapControl control)
    {
        var map = control.Map;
        map.Layers.Add(OpenStreetMap.CreateTileLayer("FieldSites/1.0"));
        var pins = new MemoryLayer("Sites")
        {
            // A fixed color for brevity: take it from the theme in a real page (skia-theme-colors.md).
            Style = new SymbolStyle { SymbolScale = 0.6, Fill = new Mapsui.Styles.Brush(Color.FromString("#6750A4")) },
        };
        map.Layers.Add(pins);
        map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(-73.57, 45.52).ToMPoint(), map.Navigator.Resolutions[12]);
        map.Info += (_, e) =>
        {
            if (e.GetMapInfo([pins]).Feature?["name"] is string name) { /* show details for name */ }
        };
        return pins;
    }

    static void ShowSites(MemoryLayer pins, IEnumerable<Site> sites)
    {
        pins.Features = sites
            .Select(s => new PointFeature(SphericalMercator.FromLonLat(s.Longitude, s.Latitude).ToMPoint()) { ["name"] = s.Name })
            .ToList();
        pins.DataHasChanged();   // redraw
    }
}
```

## Rules

- **Coordinates are projected.** `SphericalMercator.FromLonLat(longitude, latitude)` takes longitude first; `.ToMPoint()` comes from `using Mapsui.Extensions;`. Raw latitude/longitude passed as an `MPoint` puts every pin near (0, 0) off the coast of Africa.
- **Name the app in the tile user agent.** `OpenStreetMap.CreateTileLayer("FieldSites/1.0")`, or no argument for the default `user-agent-of-<process name>`. A placeholder such as `MyApp/1.0 (+https://example.com)` gets every tile answered with a 403 "Access blocked" image. OpenStreetMap's volunteer servers are for light use; an app with real traffic needs a commercial tile source (`new TileLayer(KnownTileSources.Create(...))` from BruTile, as Chefs does).
- **Write the clashing names in full in C#**: `Mapsui.UI.WinUI.MapControl` (WinUI has its own `MapControl`, which Uno does not implement) and `Mapsui.Styles.Brush`/`Mapsui.Styles.Color` (WinUI has `Brush`, and `Color` comes from `Windows.UI`).
- **Taps:** `Map.Info` fires on a tap; `e.GetMapInfo(layers)` returns the feature hit in those layers, or a `Feature` of `null`. Store what you need on the feature with the indexer (`feature["name"] = ...`).
- **Change pins** by assigning `Features` again and calling `DataHasChanged()`; mutating the list you assigned earlier does nothing.
- **Zoom** is a resolution: `map.Navigator.Resolutions[n]`, larger `n` is closer (12 is a city, 16 a street). `map.Navigator.ZoomToBox(extent)` frames a set of pins.
- **Theme:** the raster tiles do not follow dark mode; only the pin and shape styles can. Read their colors with `skia-theme-colors.md` (in this skill's `references/`).
- Give the map a size: a `*` row or an explicit `Height`. In a `StackPanel` it collapses to nothing.

## Sources

- mapsui.com, "Getting started with Uno WinUI", and the samples in the `Mapsui/Mapsui` repository (the API changed between 4.x and 5.x; trust 5.x samples only).
- Uno Chefs recipe: `uno_platform_docs_search("Chefs Mapsui interactive map")`.
