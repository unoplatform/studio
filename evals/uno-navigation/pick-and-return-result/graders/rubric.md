---
type: llm
---
The reply explains how an Uno Platform app using Uno.Extensions.Navigation opens a picker page and gets the chosen `Address` back on the calling page.
PASS if all hold:
- The caller uses a result overload on an `INavigator` (`NavigateViewModelForResultAsync<TViewModel, Address>`, `NavigateRouteForResultAsync<Address>`, `NavigateViewForResultAsync<TView, Address>` or `NavigateDataForResultAsync<TData, Address>`), passing the sender (`this`) first, and awaits the response's `.Result` (a `Task<Option<Address>>`) and unwraps the option (for example `IsSome(out var address)` or `.SomeOrDefault()`), or uses an equivalent documented helper such as `GetDataAsync<Address>`.
- The picker returns the value with `NavigateBackWithResultAsync(this, data: address)` (sender first) on its `INavigator`.
- The picker route is registered in `RegisterRoutes`, with `ResultDataViewMap<..., Address>` (or a `ViewMap`/`DataViewMap` whose result type is declared) and a `RouteMap`.
FAIL if the value travels through a static/global, a shared service set before going back, a messenger, `Frame.GoBack` plus `OnNavigatedTo`, or if the caller uses a plain (non-result) navigation call and the result is lost.
