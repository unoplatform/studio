---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
Facts the judge needs: the Visibility region navigator only works on a `Grid`, so on a `StackPanel` the region cannot navigate and the request bubbles up to the parent region (the frame), which replaces MainPage. When the region is a `Grid`, the navigator creates the routed view itself if no child is named for the route.
PASS if it identifies the `StackPanel` itself as the cause of the whole-page navigation.
FAIL if it blames only the missing `Region.Name` on the children and treats the `StackPanel` as a mere layout or sizing problem.
