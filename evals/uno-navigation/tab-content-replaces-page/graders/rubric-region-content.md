---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
Facts the judge needs: when the region is a `Grid` with `uen:Region.Navigator="Visibility"`, the framework injects the registered views itself.
PASS if the corrected content area is a `Grid` with `uen:Region.Attached="True"` and `uen:Region.Navigator="Visibility"` that is either left empty or whose pre-placed children each carry a `uen:Region.Name` matching their route, with the unnamed collapsed pages removed.
FAIL if it keeps the unnamed collapsed pages inside the region.
