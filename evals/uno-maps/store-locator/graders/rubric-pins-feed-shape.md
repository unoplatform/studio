---
type: llm
---
The reply feeds Mapsui pins on an Uno Platform MVUX page from the Model's stores.
Facts the judge needs: Mapsui's `MapControl` has no `ItemsSource`, so pins reach the map through code (often an attached property bound to a `FeedView`'s `Data`). Inside a `FeedView`, an `IListFeed<T>`'s `Data` is a mutable `ICollectionView`, not an `IImmutableList<T>`: code that casts it to `IEnumerable<T>` gets null and draws no pins, and a refresh updates the view in place without a new value. An `IFeed<IImmutableList<T>>` (or `Feed.Async` returning the list) hands the map a plain list on every value.
PASS if the map receives the pins as a plain list (an `IFeed<IImmutableList<...>>`, or code that reads the feed's value directly), or the code tolerates an `ICollectionView` (iterates a non-generic `IEnumerable` with `OfType<T>()`).
FAIL if the map's code casts a `FeedView`'s `Data` from an `IListFeed` to `IEnumerable<T>` or `IImmutableList<T>`.