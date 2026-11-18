---
type: llm
---
The reply feeds Mapsui pins on an Uno Platform MVUX page from the Model's stores.
Facts the judge needs: Mapsui's `MapControl` has no `ItemsSource`, so pins reach the map through code, often an attached property bound to a `FeedView`'s `Data`. Inside a `FeedView`, `Data` for an `IListFeed<T>` or an `IFeed<IImmutableList<T>>` is a collection view that starts empty and is filled and refreshed in place, raising `CollectionChanged` without setting the property again. Code that builds the pins only in the property-changed callback draws none.
PASS if the code that turns the bound items into map features also reruns when the bound collection changes (it subscribes to `INotifyCollectionChanged.CollectionChanged`, or equivalent), or the map gets its pins without binding a `FeedView`'s `Data` (for example code that awaits the feed's value or subscribes to the feed).
FAIL if the pins are built only when a bound property is set, with nothing that reacts to the bound collection changing in place.