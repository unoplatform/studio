---
type: llm
---
The reply explains how an Uno Platform app using Uno.Extensions.Navigation opens a picker page and gets the chosen `Address` back on the calling page.
PASS if the picker returns the value with `NavigateBackWithResultAsync(this, data: address)` (sender first) on its `INavigator`.
FAIL if the value travels through a static or global, a shared service set before going back, a messenger, or `Frame.GoBack` plus `OnNavigatedTo`.
