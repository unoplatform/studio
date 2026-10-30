---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
Facts the judge needs: the Visibility region navigator only works on a `Grid`, so on a `StackPanel` the region cannot navigate and the request bubbles up to the parent region (the frame), which replaces MainPage. When the region is a `Grid`, the navigator creates the routed view itself if no child is named for the route.
PASS if it says the `StackPanel` is why the whole page is replaced: the Visibility navigator cannot navigate on it, so the request reaches the Frame.
FAIL if it gives the unnamed or pre-placed children as the cause of the replacement and mentions the `StackPanel` only as a further thing to change.
