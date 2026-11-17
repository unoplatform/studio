---
type: llm
---
The reply answers how to remove an item from a list in an Uno Platform MVUX model so the screen updates.
PASS if the model is a `partial record` whose list is an `IListState<T>` created with `ListState.Async(this, ...)` (or `ListState` from a feed).
FAIL if the list is a property that is re-assigned to refresh, an `ObservableCollection`, or lives in a ViewModel class with `INotifyPropertyChanged`.
