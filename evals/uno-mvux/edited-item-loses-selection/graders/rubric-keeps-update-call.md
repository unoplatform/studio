---
type: llm
---
The reply fixes an MVUX list item that is removed and re-added on edit in an Uno Platform app.
PASS if the update call can stay as written (`UpdateAllAsync` or `UpdateItemAsync`).
FAIL if it replaces the list state with an `ObservableCollection`, mutates a `List<T>` in place, or reloads the whole list to work around the problem.
