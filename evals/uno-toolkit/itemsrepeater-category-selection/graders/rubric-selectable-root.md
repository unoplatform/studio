---
type: llm
---
The reply adds single selection, bound to `SelectedCategory`, to an `ItemsRepeater` of category pills in an Uno Platform app.
Facts the judge needs: the selection visual works only when the ItemTemplate's root element is a `SelectorItem` or a `ToggleButton` (which includes `utu:Chip`, `RadioButton`, `CheckBox`, `ToggleButton`, `ListViewItem`); the extension sets `IsChecked`/`IsSelected` itself.
PASS if the ItemTemplate root is a toggleable or selectable element such as `utu:Chip`, `ToggleButton`, `RadioButton` or `ListViewItem`, so the selected pill looks selected without extra bindings.
FAIL if the item root (a `Border`, `Grid` or `TextBlock`) cannot show selection.
