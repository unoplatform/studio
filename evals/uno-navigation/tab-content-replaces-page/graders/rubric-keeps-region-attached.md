---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
PASS if it keeps `uen:Region.Attached="True"` on the TabBar and on the shared parent Grid, and the `Region.Name` values still match the routes.
FAIL if it removes `Region.Attached` from the TabBar or the parent Grid, or renames tabs so they no longer match the routes.
