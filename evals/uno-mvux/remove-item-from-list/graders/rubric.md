---
type: llm
---
The reply answers how to remove an item from a list in an Uno Platform MVUX model so the screen updates.
PASS if all hold:
- The model is a `partial record` whose list is an `IListState<T>` created with `ListState.Async(this, ...)` (or `ListState` from a feed), not a property that is re-assigned.
- Removal goes through the list state (`RemoveAllAsync(predicate, ct)` or `UpdateAsync`) after, or alongside, the service call; the page is not reloaded or re-navigated to refresh the list.
- The remove action is a public method on the model (an MVUX implicit command) or `Command.Create`, not a code-behind Click handler.
FAIL if it uses `ObservableCollection`, `INotifyPropertyChanged`, a ViewModel class, or refreshes by re-assigning the list.
