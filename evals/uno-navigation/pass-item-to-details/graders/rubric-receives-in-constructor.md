---
type: llm
---
The reply explains how, in an Uno Platform app using Uno.Extensions.Navigation, tapping an `Order` in a list opens a details page that shows that order.
PASS if the details model or ViewModel receives the `Order` as a constructor parameter, injected by navigation.
FAIL if the order arrives through a static field, a shared service set before navigating, `OnNavigatedTo`/`NavigationEventArgs.Parameter`, or a query-string or ID lookup.
