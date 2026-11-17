---
type: llm
---
The reply fixes an MVUX list item that is removed and re-added on edit in an Uno Platform app.
Facts the judge needs: MVUX diffs list feeds and list states with key equality, `Uno.Extensions.Equality.IKeyEquatable<T>`. It is generated for a `partial record` that has a property named `Id` or `Key`, or whose key property carries `[Key]` (`[property: Key]` on a positional record); `[assembly: ImplicitKeys(...)]` changes the implicit names. Without a key, an edited record (new value, so not equal) is diffed as a remove plus an add.
PASS if it marks `Sku` as `Product`'s key (`[property: Key]`, implementing `IKeyEquatable<Product>`, `ImplicitKeys`, or renaming to `Id` all pass).
FAIL if it keys a different property, or invents an interface other than `IKeyEquatable<T>`.
