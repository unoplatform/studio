---
type: llm
---
The reply explains how to add Hot Design previews of an MVUX `ProductsPage` that show it with data, loading, with no products, and when the service fails.
Facts the judge needs: the mock builders are `ListFeedMock.Value(...)`, `ListFeedMock.Loading<T>()`, `ListFeedMock.Empty<T>()` and `ListFeedMock.Error<T>(exception)` (from `Uno.HotTesting.Reactive`), passed to `ProductsViewModelMock.Create(new ProductsModelMock { Items = ... })` or set with `ProductsModelMock.Empty with { ... }`.
PASS if it defines a preview for each of the four states, each over a mock whose `Items` uses the matching `ListFeedMock` builder.
FAIL if a state is missing, or a state is faked with a hand-written service or view-model instead of the mock.
