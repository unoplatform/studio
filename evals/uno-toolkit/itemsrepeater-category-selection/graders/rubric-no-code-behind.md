---
type: llm
---
The reply adds single selection, bound to `SelectedCategory`, to an `ItemsRepeater` of category pills in an Uno Platform app.
PASS if selection needs no code-behind.
FAIL if it adds a code-behind Tapped or Click handler, or a hand-rolled `IsSelected` flag on each item.
