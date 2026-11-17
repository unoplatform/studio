---
type: llm
---
The reply explains how, in an Uno Platform app using Uno.Extensions.Navigation, tapping an `Order` in a list opens a details page that shows that order.
PASS if navigation passes the order itself: `uen:Navigation.Request` on the `ListView` (which sends the clicked item as data), `uen:Navigation.Data="{Binding ...}"` on an item element in XAML, or `NavigateDataAsync(this, order)` (or `NavigateRouteAsync`/`NavigateViewModelAsync` with a `data:` argument) on an `INavigator`, with the sender passed first.
FAIL if it uses `Frame.Navigate(typeof(...), order)` or passes only an ID to look the order up again.
