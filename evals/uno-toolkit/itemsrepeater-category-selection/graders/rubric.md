---
type: llm
---
The reply adds single selection, bound to `SelectedCategory`, to an `ItemsRepeater` of category pills in an Uno Platform app.
Background (Uno Toolkit docs, ItemsRepeater Extensions): `utu:ItemsRepeaterExtensions.SelectedItem`/`SelectedIndex` only take effect when `utu:ItemsRepeaterExtensions.SelectionMode` is set to something other than `None` (`Single` or `SingleOrNone` for one item). The selection visual works only when the ItemTemplate's root element is a `SelectorItem` or a `ToggleButton` (which includes `utu:Chip`, `RadioButton`, `CheckBox`, `ToggleButton`, `ListViewItem`); the extension sets `IsChecked`/`IsSelected` itself.
PASS if all hold:
- It keeps the `ItemsRepeater` and uses `utu:ItemsRepeaterExtensions.SelectionMode="Single"` (or `SingleOrNone`) together with `utu:ItemsRepeaterExtensions.SelectedItem="{Binding SelectedCategory, Mode=TwoWay}"` (or an equivalent two-way binding, or an MVUX state).
- The ItemTemplate root is a toggleable/selectable element such as `utu:Chip`, `ToggleButton`, `RadioButton`, or `ListViewItem`, so the selected pill looks selected without extra bindings.
- No code-behind Tapped/Click handler or hand-rolled `IsSelected` flag on each item is needed for selection.
FAIL if it replaces the `ItemsRepeater` with a `ListView`/`GridView`, tracks selection in code-behind, omits `SelectionMode`, or uses an item root (Border, Grid, TextBlock) that cannot show selection.
