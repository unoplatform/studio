---
type: llm
---
The reply answers how to remove an item from a list in an Uno Platform MVUX model so the screen updates.
PASS if removal goes through the list state (`RemoveAllAsync(predicate, ct)` or `UpdateAsync`) after, or alongside, the service call.
FAIL if it reloads the page or navigates again to refresh the list.
