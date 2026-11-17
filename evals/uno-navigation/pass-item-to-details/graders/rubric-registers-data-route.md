---
type: llm
---
The reply explains how, in an Uno Platform app using Uno.Extensions.Navigation, tapping an `Order` in a list opens a details page that shows that order.
PASS if the details route is registered in `RegisterRoutes` (App.xaml.cs) with a `DataViewMap<OrderDetailsPage, OrderDetailsModel, Order>` (or `ResultDataViewMap`) plus a `RouteMap` entry.
FAIL if the route is registered without its data type, or not registered at all.
