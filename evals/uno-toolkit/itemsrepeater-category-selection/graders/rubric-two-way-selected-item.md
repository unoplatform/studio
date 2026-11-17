---
type: llm
---
The reply adds single selection, bound to `SelectedCategory`, to an `ItemsRepeater` of category pills in an Uno Platform app.
Facts the judge needs: `utu:ItemsRepeaterExtensions.SelectedItem`/`SelectedIndex` only take effect when `utu:ItemsRepeaterExtensions.SelectionMode` is set to something other than `None` (`Single` or `SingleOrNone` for one item).
PASS if the selection is bound two-way to `SelectedCategory` through `utu:ItemsRepeaterExtensions.SelectedItem` (`Mode=TwoWay`, an equivalent two-way binding, or an MVUX state).
FAIL if the binding is one-way, or the selected category is tracked some other way.
