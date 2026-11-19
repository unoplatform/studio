# Map basics: position, shapes, location

**Contents:** [Coordinates](#coordinates) · [Center and zoom](#center-and-zoom) · [A route line](#a-route-line) · [A shaded area](#a-shaded-area) · [Labels](#labels) · [The user's location](#the-users-location) · [Size](#size)

Usings for this page: `Mapsui`, `Mapsui.Extensions`, `Mapsui.Layers`, `Mapsui.Nts`, `Mapsui.Nts.Extensions`, `Mapsui.Projections`, `Mapsui.Styles`, `NetTopologySuite.Geometries`, and the alias `using MapControl = Mapsui.UI.WinUI.MapControl;`.

## Coordinates

Mapsui draws in Spherical Mercator meters, not degrees. Convert every latitude/longitude from the Model, longitude first:

```csharp
MPoint point = SphericalMercator.FromLonLat(site.Longitude, site.Latitude).ToMPoint();   // pins, centering
Coordinate c = SphericalMercator.FromLonLat(site.Longitude, site.Latitude).ToCoordinate(); // lines, areas
var (lon, lat) = SphericalMercator.ToLonLat(point.X, point.Y);                            // back, e.g. for a tap
```

`ToMPoint()` is in `Mapsui.Extensions`, `ToCoordinate()` in `Mapsui.Nts.Extensions`.

## Center and zoom

```csharp
var navigator = control.Map.Navigator;
navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(-73.57, 45.52).ToMPoint(), navigator.Resolutions[12]);
navigator.ZoomToLevel(15);
navigator.ZoomToBox(pinsLayer.Extent!.Grow(500), MBoxFit.Fit);
```

Zoom is a resolution: `Resolutions[n]` with a larger `n` is closer (about 4 is a continent, 12 a city, 16 a street). `Resolutions` comes from the first tile layer, so add it before navigating. Calls made before the control has a size are queued and run once it has one, so they can go in the constructor or the first data callback.

## A route line

```csharp
var line = new LineString(stops.Select(p => SphericalMercator.FromLonLat(p.Longitude, p.Latitude).ToCoordinate()).ToArray());
var route = new MemoryLayer("Route")
{
    Features = [new GeometryFeature(line)],
    Style = new VectorStyle { Line = new Pen(color, 4) },
};
control.Map.Layers.Add(route);   // after the tiles, before the pins: pins sit on the line
```

`LineString` needs at least two points. A road route (turn by turn) comes from a routing API as a list of points; Mapsui only draws it.

## A shaded area

```csharp
var ring = border.Select(p => SphericalMercator.FromLonLat(p.Lon, p.Lat).ToCoordinate()).ToList();
ring.Add(ring[0]);   // a ring is closed: last point = first point
var zone = new MemoryLayer("Zone")
{
    Features = [new GeometryFeature(new Polygon(new LinearRing(ring.ToArray())))],
    Style = new VectorStyle
    {
        Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(64, 103, 80, 164)),
        Outline = new Pen(Mapsui.Styles.Color.FromArgb(255, 103, 80, 164), 2),
    },
};
```

An unclosed ring throws `ArgumentException` from NetTopologySuite.

## Labels

A layer can draw a symbol and a label from a feature field:

```csharp
pins.Style = new StyleCollection
{
    Styles =
    {
        new SymbolStyle { SymbolScale = 0.7 },
        new LabelStyle { LabelColumn = "label", Offset = new Offset(0, -18) },
    },
};
// on each feature: feature["label"] = pin.Label;
```

Labels on many pins overlap: keep them for a handful, and show the selected pin's details in XAML next to the map instead.

## The user's location

Mapsui draws the position; it does not read the GPS. Get the position from `Geolocator` (`Windows.Devices.Geolocation`; the platforms and the permission each head needs: `uno_platform_docs_search("Geolocation Geolocator")`) and feed it in:

```csharp
var me = new MyLocationLayer(control.Map) { Enabled = true };
control.Map.Layers.Add(me);
me.UpdateMyLocation(SphericalMercator.FromLonLat(lon, lat).ToMPoint());
me.UpdateMyDirection(headingDegrees, control.Map.Navigator.Viewport.Rotation);
```

## Size

`MapControl` takes the space it is given and has no size of its own:

- In a `Grid`, a `*` row or column, or a fixed `Height`. An `Auto` row collapses it to nothing.
- In a `StackPanel` or `ScrollViewer` it gets no height: give it an explicit `Height`.
- In a `FeedView` or a card, set `HorizontalContentAlignment` and `VerticalContentAlignment` to `Stretch`.
