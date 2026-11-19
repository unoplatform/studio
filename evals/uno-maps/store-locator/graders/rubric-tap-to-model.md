---
type: llm
---
The reply shows Mapsui pins for stores on an Uno Platform MVUX page, with a tap that shows the tapped store's name.
Facts the judge needs: Mapsui's `MapControl` has no `ItemsSource`; pins are `PointFeature`s in a `MemoryLayer` built in code from the Model's data (often through an attached property or page code that receives the feed's value). A tap raises `Map.Info` (or `MapControl.Info`); the handler finds the tapped feature with `e.GetMapInfo(layers)` (or `e.MapInfo`), reads the store stored on it, and hands it to the Model (a command or a state update), which the name `TextBlock` binds to.
PASS if the tapped store reaches the Model (a command, method or state) and the name shown above the map comes from a binding to it.
FAIL if there is no tap handling, or the tap sets a `TextBlock.Text` directly in code-behind with no Model involved.