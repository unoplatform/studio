# Pins from an MVUX feed

`MapControl` has no `ItemsSource`: pins live in a `MemoryLayer` built in code. An attached property bridges the two, also inside a `FeedView` template where the map has no `x:Name` field.

**Contents:** [Model](#model) · [XAML](#xaml) · [The attached property](#the-attached-property) · [Rules](#rules)

## Model

```csharp
public partial record MapPin(string Id, string Label, double Latitude, double Longitude);

public partial record SitesModel(ISiteService Sites)
{
    public IListFeed<MapPin> Pins => ListFeed.Async(async ct =>
        (IImmutableList<MapPin>)(await Sites.GetSites(ct))
            .Select(s => new MapPin(s.Id, s.Name, s.Latitude, s.Longitude))
            .ToImmutableList());

    public IState<MapPin> Selected => State<MapPin>.Empty(this);

    public async ValueTask Select(MapPin pin, CancellationToken ct) => await Selected.UpdateAsync(_ => pin, ct);
}
```

A record with an `Id` must be `partial` (`KE0001`).

`IListFeed<MapPin>` and `IFeed<IImmutableList<MapPin>>` behave the same here: inside a `FeedView`, `Data` is a collection view that starts empty, then fills and refreshes in place. Code that reads it only when the binding sets it, or casts it to `IEnumerable<MapPin>`, shows no pins and no error, on a map framing the whole world.

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

`Parent` is the page's ViewModel, where the generated `Select` command lives.

## The attached property

```csharp
using System.Collections.Specialized;
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
        "Items", typeof(object), typeof(MapPins), new PropertyMetadata(null, OnItemsChanged));
    public static object? GetItems(DependencyObject d) => d.GetValue(ItemsProperty);
    public static void SetItems(DependencyObject d, object? value) => d.SetValue(ItemsProperty, value);

    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.RegisterAttached(
        "SelectCommand", typeof(ICommand), typeof(MapPins), new PropertyMetadata(null));
    public static ICommand? GetSelectCommand(DependencyObject d) => (ICommand?)d.GetValue(SelectCommandProperty);
    public static void SetSelectCommand(DependencyObject d, ICommand? value) => d.SetValue(SelectCommandProperty, value);

    static readonly ConditionalWeakTable<MapControl, PinLayer> Layers = new();

    sealed class PinLayer(MemoryLayer pins)
    {
        public MemoryLayer Pins { get; } = pins;
        public NotifyCollectionChangedEventHandler? ItemsChanged { get; set; }
    }

    // The view fills in place: listen to it, not only to the property.
    static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MapControl control) return;
        var layer = Layers.GetValue(control, Create);
        if (e.OldValue is INotifyCollectionChanged oldItems) oldItems.CollectionChanged -= layer.ItemsChanged;
        if (e.NewValue is INotifyCollectionChanged newItems) newItems.CollectionChanged += layer.ItemsChanged;
        Update(control, layer);
    }

    static void Update(MapControl control, PinLayer layer)
    {
        layer.Pins.Features = ((GetItems(control) as System.Collections.IEnumerable)?.OfType<MapPin>() ?? [])
            .Select(p => new PointFeature(SphericalMercator.FromLonLat(p.Longitude, p.Latitude).ToMPoint()) { ["pin"] = p })
            .ToList();
        layer.Pins.DataHasChanged();
        if (layer.Pins.Extent is { } extent)
        {
            // Queued by Mapsui until the control has a size.
            control.Map.Navigator.ZoomToBox(extent.Grow(extent.Width * 0.2 + 500), MBoxFit.Fit);
        }
    }

    static PinLayer Create(MapControl control)
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
        var layer = new PinLayer(pins);
        layer.ItemsChanged = (_, _) => Update(control, layer);
        return layer;
    }
}
```

Theme colors: `references/tiles-and-colors.md`. A route: `references/map-basics.md`.

## Rules

- **Never bind `Items` to the feed itself** (`{Binding Pins}` outside a `FeedView`): bind `{Binding Data}` inside it.
- **Assign `Features` again and call `DataHasChanged()`**: mutating the assigned list redraws nothing.
- **`Map.Info` fires on a tap**; `e.GetMapInfo(layers).Feature` is the hit, or `null`. Store the record on the feature (`feature["pin"] = pin`), not just its id.
- **Add the tile layer first**: layers draw in the order added.
- **Frame with `Navigator.ZoomToBox(extent)`** plus a margin: one pin has a zero extent.
