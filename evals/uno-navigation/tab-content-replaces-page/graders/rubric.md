---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
PASS if all hold:
- It identifies the `StackPanel` itself as the cause of the whole-page navigation: the Visibility region navigator only works on a `Grid`, so on a `StackPanel` the region cannot navigate and the request bubbles up to the parent region (the frame), which replaces MainPage. A reply that blames only the missing `Region.Name` on the children and treats the `StackPanel` as a mere layout/sizing problem does not meet this point (when the region is a `Grid`, the navigator creates the routed view itself if no child is named for the route).
- The corrected content area is a `Grid` with `uen:Region.Attached="True"` and `uen:Region.Navigator="Visibility"` that is left empty (the framework injects the registered Home/Cart/Profile views), or whose pre-placed children each carry a `uen:Region.Name` matching their route; the unnamed collapsed pages are removed.
- It keeps `uen:Region.Attached="True"` on the TabBar and on the shared parent Grid, and the `Region.Name` values still match the routes.
FAIL if the fix toggles page visibility or swaps content in code-behind (SelectionChanged handlers, `Frame.Navigate`), or keeps the unnamed collapsed pages inside the region.
