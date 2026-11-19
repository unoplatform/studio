---
type: llm
---
The reply explains how to add Hot Design previews of an MVUX `ProductsPage` that show it with data, loading, with no products, and when the service fails.
Facts the judge needs: the mock builders are `ListFeedMock.Value(...)`, `ListFeedMock.Loading<T>()`, `ListFeedMock.Empty<T>()` and `ListFeedMock.Error<T>(exception)` (from `Uno.HotTesting.Reactive`), passed to `ProductsViewModelMock.Create(new ProductsModelMock { Items = ... })` or set with `ProductsModelMock.Empty with { ... }`. `ProductsModelMock.Empty` on its own pins every feed to empty, so `ProductsViewModelMock.Create(ProductsModelMock.Empty)` is a correct Empty state.
PASS if it defines a preview for each of the four states, each over a generated mock set to that state.
FAIL if a state is missing, or a state is faked with a hand-written service or view-model instead of the mock.
