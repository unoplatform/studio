---
type: llm
---
The reply explains how, in an Uno Platform app using Uno.Extensions.Navigation, tapping an `Order` in a list opens a details page that shows that order.
PASS if all hold:
- The details route is registered in `RegisterRoutes` (App.xaml.cs) with a `DataViewMap<OrderDetailsPage, OrderDetailsModel, Order>` (or `ResultDataViewMap`) plus a `RouteMap` entry, so the framework knows which data type the page takes.
- Navigation passes the order itself: `uen:Navigation.Request` on the `ListView` (which sends the clicked item as data) or with `uen:Navigation.Data="{Binding ...}"` on an item element in XAML, or `NavigateDataAsync(this, order)` (or `NavigateRouteAsync`/`NavigateViewModelAsync` with a `data:` argument) on an `INavigator`, with the sender passed first.
- The details model/ViewModel receives the `Order` as a constructor parameter (injected by navigation), not through a static field, a shared service set before navigating, `OnNavigatedTo`, or a query-string/ID lookup hack.
FAIL if it uses `Frame.Navigate(typeof(...), order)`, reads `NavigationEventArgs.Parameter`, or stores the selected order in a global/static to hand it over.
