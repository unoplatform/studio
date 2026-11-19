# Tiles and colors

**Contents:** [OpenStreetMap tiles](#openstreetmap-tiles) · [A keyed tile provider](#a-keyed-tile-provider) · [Pin and route colors from the theme](#pin-and-route-colors-from-the-theme)

## OpenStreetMap tiles

`OpenStreetMap.CreateTileLayer()` loads `tile.openstreetmap.org`: free, no key, and the default for a prototype. Its use policy (operations.osmfoundation.org/policies/tiles):

- **Attribution is required.** The layer carries "© OpenStreetMap contributors", and Mapsui draws a layer's attribution on the map. Keep it visible: no overlay on that corner.
- **Identify the app.** `CreateTileLayer("FieldSites/1.0")` sends that user agent; with no argument Mapsui sends `user-agent-of-<process name>`. Never copy a placeholder such as `MyApp/1.0`: the policy lets the servers block generic or faked agents.
- **Light use only.** The volunteer servers are not for an app with real traffic, offline bulk download, or prefetching. A shipped app uses a keyed provider.

The raster tiles do not follow dark mode: the map stays light in a dark app. A provider's dark style (a second URL) can be swapped in on `ActualThemeChanged`.

## A keyed tile provider

MapTiler, Stadia Maps, Thunderforest and others serve the same `{z}/{x}/{y}` URLs with a key:

```csharp
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;

var source = new HttpTileSource(
    new GlobalSphericalMercator(),
    "https://api.maptiler.com/maps/streets-v2/{z}/{x}/{y}.png?key=" + apiKey,
    name: "MapTiler Streets",
    attribution: new BruTile.Attribution("© MapTiler © OpenStreetMap contributors", "https://www.maptiler.com/copyright/"));
control.Map.Layers.Insert(0, new TileLayer(source));
```

- Read the key from configuration (`appsettings.json` through `IOptions<T>`), never a literal in code; a key shipped in an app is public, so restrict it by app or domain in the provider's dashboard.
- Put the provider's own attribution text in `attribution`.
- WebAssembly loads tiles from the browser: the provider must send `Access-Control-Allow-Origin`. OpenStreetMap and the large providers do; check a self-hosted tile server before shipping.

## Pin and route colors from the theme

Mapsui paints with its own `Mapsui.Styles.Color`, which `{ThemeResource}` never reaches. Read the theme's color for the map's `ActualTheme`, and paint again when it changes. `Application.Current.Resources["PrimaryColor"]` is wrong: after a switch to dark mode it still answers with the light value. WinUI keys the dark dictionary `"Default"`.

```csharp
using MapsuiColor = Mapsui.Styles.Color;

static class ThemeColors
{
    public static MapsuiColor Get(FrameworkElement element, string key)
    {
        string[] themes = element.ActualTheme == ElementTheme.Dark ? ["Dark", "Default"] : ["Light"];
        var c = themes.Select(t => Find(Application.Current.Resources, t, key)).OfType<Windows.UI.Color>().FirstOrDefault();
        return new MapsuiColor(c.R, c.G, c.B, c.A);
    }

    static object? Find(ResourceDictionary dictionary, string theme, string key)
    {
        if (dictionary.ThemeDictionaries.TryGetValue(theme, out var t) && t is ResourceDictionary td && td.TryGetValue(key, out var v))
        {
            return v;
        }
        for (var i = dictionary.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (Find(dictionary.MergedDictionaries[i], theme, key) is { } found)
            {
                return found;
            }
        }
        return null;
    }
}
```

In the `Create` method of `MapPins` (`references/pins-from-feeds.md`), paint the layers once and on every theme change:

```csharp
void Paint()
{
    pins.Style = new SymbolStyle
    {
        SymbolScale = 0.7,
        Fill = new Mapsui.Styles.Brush(ThemeColors.Get(control, "PrimaryColor")),
        Outline = new Pen(ThemeColors.Get(control, "OnPrimaryColor"), 2),
    };
    control.Map.RefreshGraphics();
}
Paint();
control.ActualThemeChanged += (_, _) => Paint();
```

- Keys are the Uno Themes keys (Material and Simple): `PrimaryColor` and `OnPrimaryColor` for pins, `TertiaryColor` for a route, `ErrorColor` for an alert pin (the `uno-themes` skill, `references/semantic-colors-brushes.md`). An app without Uno Themes has none of these keys, and the helper returns transparent: the pins vanish.
- Pins in categories (a status, a type) must differ by more than a key. In the Simple theme `PrimaryColor` and `TertiaryColor` are both neutral (black and gray in light, white and gray in dark), so two categories painted with them look alike next to `ErrorColor`'s red. Give each category a different `SymbolScale` or `SymbolType` too, or draw a legend beside the map.
- A selected pin gets its own style on its feature (`feature.Styles.Add(...)`) in a second color.
- Keep colors out of the Model.
- Check: switch the running app to dark mode (`SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme.Dark)` from Uno Toolkit, or the OS setting) and look at the pins.
