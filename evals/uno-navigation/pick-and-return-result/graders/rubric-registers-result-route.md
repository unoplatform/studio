---
type: llm
---
The reply explains how an Uno Platform app using Uno.Extensions.Navigation opens a picker page and gets the chosen `Address` back on the calling page.
PASS if the picker route is registered in `RegisterRoutes`, with `ResultDataViewMap<..., Address>` (or a `ViewMap`/`DataViewMap` whose result type is declared) and a `RouteMap`.
FAIL if the picker route is not registered, or registered without its result type.
