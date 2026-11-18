# Pins from an MVUX feed

`MapControl` has no `ItemsSource` and no bindable data property: pins live in a `MemoryLayer` built in code. An attached property bridges the two, so the map binds like any control, also inside a `FeedView` template where it has no `x:Name` field.

**Contents:** [Model](#model) · [XAML](#xaml) · [The attached property](#the-attached-property) · [Rules](#rules)

## Model

```csharp
public partial record MapPin(string Id, string Label, double Latitude, double Longitude);

public partial record SitesModel(ISiteService Sites)
{
    public IFeed<IImmutableList<MapPin>> Pins => Feed.Async(async ct =>
        (IImmutableList<MapPin>)(await Sites.GetSites(ct))
            .Select(s => new MapPin(s.Id, s.Name, s.Latitude, s.Longitude))
            .ToImmutableList());

    public IState<MapPin> Selected => State<MapPin>.Empty(this);

    public async ValueTask Select(MapPin pin, CancellationToken ct) => await Selected.UpdateAsync(_ => pin, ct);
}
```

A record with an `Id` must be `partial`, or the build fails with `KE0001`.

## XAML

```xml
<Page xmlns:mapsui="using:Mapsui.UI.WinUI"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI" ...>
  <Grid RowDefinitions="Auto,*">
    <TextBlock Text="{Binding Selected.Label, FallbackValue='Tap a pin'}" />
    <mvux:FeedView Grid.Row="1" Source="{Binding Pins}"
                   HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch">
      <DataTemplate>
        <mapsui:MapControl AutomationProperties.AutomationId="SitesMap"
                           local:MapPins.Items="{Binding Data}"
                           local:MapPins.SelectCommand="{Binding Parent.Select}" />
      </DataTemplate>
    </mvux:FeedView>
  </Grid>
</Page>
```

`Data` is the feed's current value; `Parent` is the page's ViewModel, where the generated `Select` command lives. The `FeedView` shows loading and error states, and a refresh hands the map a new list.

## The attached property

```csharp
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using MapControl = Mapsui.UI.WinUI.MapControl;

public static class MapPins
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.RegisterAttached(
        "Items", typeof(object), typeof(MapPins), new PropertyMetadata(null, (d, _) => Update(d)));
    public static object? GetItems(DependencyObject d) => d.GetValue(ItemsProperty);
    public static void SetItems(DependencyObject d, object? value) => d.SetValue(ItemsProperty, value);

    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.RegisterAttached(
        "SelectCommand", typeof(ICommand), typeof(MapPins), new PropertyMetadata(null));
    public static ICommand? GetSelectCommand(DependencyObject d) => (ICommand?)d.GetValue(SelectCommandProperty);
    public static void SetSelectCommand(DependencyObject d, ICommand? value) => d.SetValue(SelectCommandProperty, value);

    // One pin layer per map, created with the map's first data.
    static readonly ConditionalWeakTable<MapControl, MemoryLayer> Layers = new();

    static void Update(DependencyObject d)
    {
        if (d is not MapControl control) return;
        var layer = Layers.GetValue(control, Create);
        layer.Features = ((GetItems(control) as IEnumerable<MapPin>) ?? [])
            .Select(p => new PointFeature(SphericalMercator.FromLonLat(p.Longitude, p.Latitude).ToMPoint()) { ["pin"] = p })
            .ToList();
        layer.DataHasChanged();
        if (layer.Extent is { } extent)
        {
            // Queued by Mapsui until the control has a size.
            control.Map.Navigator.ZoomToBox(extent.Grow(extent.Width * 0.2 + 500), MBoxFit.Fit);
        }
    }

    static MemoryLayer Create(MapControl control)
    {
        var map = control.Map;
        map.Layers.Add(OpenStreetMap.CreateTileLayer("FieldSites/1.0"));
        var pins = new MemoryLayer("Pins");
        map.Layers.Add(pins);
        map.Info += (_, e) =>
        {
            if (e.GetMapInfo([pins]).Feature?["pin"] is MapPin pin && GetSelectCommand(control) is { } command && command.CanExecute(pin))
            {
                command.Execute(pin);
            }
        };
        return pins;
    }
}
```

Pin and route colors from the theme: `references/tiles-and-colors.md`. A route line through the pins: `references/map-basics.md`.

## Rules

- **Never bind `Items` to the feed itself** (`{Binding Pins}` outside a `FeedView`): the attached property receives the feed wrapper, not the list. Bind `{Binding Data}` inside the `FeedView`.
- **Assign `Features` again and call `DataHasChanged()`** to change pins; mutating a list already assigned redraws nothing.
- **`Map.Info` fires on a tap**; `e.GetMapInfo(layers)` returns the feature hit in those layers, or a `Feature` of `null`. Store the record on the feature with the indexer (`feature["pin"] = pin`), not just its id.
- **Add the tile layer first**: layers draw in the order added, so pins added before the tiles are hidden under them.
- **Frame with `Navigator.ZoomToBox(extent)`**, grown by a margin; a single pin has an extent of zero, hence the `+ 500` meters.
