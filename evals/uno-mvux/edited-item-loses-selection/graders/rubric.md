---
type: llm
---
The reply fixes an MVUX list item that is removed and re-added on edit in an Uno Platform app.
Facts the judge needs: MVUX diffs list feeds and list states with key equality, `Uno.Extensions.Equality.IKeyEquatable<T>`. It is generated for a `partial record` that has a property named `Id` or `Key`, or whose key property carries `[Key]` (`[property: Key]` on a positional record); `[assembly: ImplicitKeys(...)]` changes the implicit names. Without a key, an edited record (new value, so not equal) is diffed as a remove plus an add.
PASS if all hold:
- It names missing key equality / identity as the cause, not the update call.
- It makes `Product` a `partial record` and marks `Sku` as its key (`[property: Key]`, implementing `IKeyEquatable<Product>`, `ImplicitKeys`, or renaming to `Id` all pass).
- The update call can stay as written (`UpdateAllAsync` or `UpdateItemAsync`); it does not replace the list state with an `ObservableCollection` or mutate a `List<T>` in place.
FAIL if it invents an interface other than `IKeyEquatable<T>`, recommends overriding `Equals`/`GetHashCode` as the fix, or reloads the whole list to work around it.
