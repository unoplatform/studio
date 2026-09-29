# Silverlight to Uno Platform

Silverlight shares most of its XAML and API surface with WPF, so the WPF references apply to it: `references/wpf-api-mapping.md` for namespaces and APIs, `references/wpf-xaml.md` for XAML and controls. This file covers the subsystems Silverlight has and WPF does not. None of them has a WinUI equivalent, so each one is a redesign.

| Silverlight | Uno Platform replacement |
|---|---|
| `NavigationService` with URI fragments | Uno.Extensions Navigation with registered routes |
| `NavigationContext.QueryString` | Navigation data (`uen:Navigation.Data`, `NavigateRouteAsync(data:)`) |
| WCF RIA Services (`DomainService`, `DomainContext`) | An HTTP API with a Kiota or Refit client |
| `IsolatedStorageFile`, `IsolatedStorageSettings` | `ApplicationData.Current.LocalFolder`, settings through `IWritableOptions<T>` |
| `ChildWindow` | `ContentDialog`, or a dialog route |
| Silverlight Toolkit controls | WinUI built-ins and Uno Toolkit, control by control |

## Navigation

Silverlight navigates by URI and reads parameters from a query-string bag:

```csharp
NavigationService.Navigate(new Uri("/Views/OrderDetails.xaml?orderId=123", UriKind.Relative));

protected override void OnNavigatedTo(NavigationEventArgs e)
{
    var orderId = NavigationContext.QueryString["orderId"];
}
```

With Uno.Extensions Navigation, register a route per page and pass a typed object. The route's view model receives it through its constructor:

```csharp
views.Register(new DataViewMap<OrderDetailsPage, OrderDetailsViewModel, Order>());
routes.Register(new RouteMap("OrderDetails", View: views.FindByViewModel<OrderDetailsViewModel>()));

// Caller
await navigator.NavigateRouteAsync(this, "OrderDetails", data: selectedOrder);

// Target
public partial class OrderDetailsViewModel(Order order) : ObservableObject { }
```

```xml
<Button Content="View order"
        uen:Navigation.Request="OrderDetails"
        uen:Navigation.Data="{Binding SelectedOrder}" />
```

Before writing routes, read the `uno-navigation` skill (`references/data.md` and `references/routes.md`). Do not rebuild the query-string bag with static fields or a singleton.

## Data access

WCF RIA Services has no .NET successor. The data layer becomes an HTTP API, and the client is generated or declared:

- **Kiota** when the API publishes an OpenAPI document.
- **Refit** when you declare the contract as a C# interface.

Both register through Uno.Extensions HTTP (`<UnoFeatures>Http</UnoFeatures>` plus the Kiota or Refit feature):

```csharp
public interface IOrderApi
{
    [Get("/api/orders")]
    Task<List<Order>> GetOrdersAsync(CancellationToken ct = default);
}

.UseHttp((context, services) => services.AddRefitClient<IOrderApi>(context))
```

The endpoint's base address comes from configuration (`appsettings.json`), not a literal in code. Replace RIA's `Load(query, callback)` pattern with `async`/`await`; never block on the result.

If the server side is also being rewritten, an ASP.NET Core Web API that publishes OpenAPI gives you a Kiota client for free.

## Storage

`IsolatedStorageFile` becomes `ApplicationData.Current.LocalFolder`:

```csharp
// Silverlight
var store = IsolatedStorageFile.GetUserStoreForApplication();
using var stream = store.CreateFile("orders.json");

// Uno Platform
var folder = ApplicationData.Current.LocalFolder;
var file = await folder.CreateFileAsync("orders.json", CreationCollisionOption.ReplaceExisting);
await FileIO.WriteTextAsync(file, json);
```

`IsolatedStorageSettings.ApplicationSettings` maps to settings through `IWritableOptions<T>`; see `references/wpf-architecture.md`. On WebAssembly the local folder lives in browser storage, so test size limits there.

## ChildWindow

A `ChildWindow` becomes a `ContentDialog` (set `XamlRoot` before `ShowAsync()`), or a dialog route when the app uses Uno.Extensions Navigation (the `uno-navigation` skill, `references/dialogs.md`). Do not rebuild it as a custom `Popup` overlay.

## Silverlight Toolkit controls

| Silverlight Toolkit | Uno Platform |
|---|---|
| `AutoCompleteBox` | `AutoSuggestBox` |
| `BusyIndicator` | Toolkit `LoadingView`, or `ProgressRing` |
| `NumericUpDown` | `NumberBox` |
| `Rating` | `RatingControl` |
| `DatePicker`, `TimePicker` | `CalendarDatePicker` or `DatePicker`, `TimePicker` |
| `TabControl` | `TabView`, or Toolkit `TabBar` |
| `TreeView` | `TreeView` |
| `Accordion` | A list of `Expander` controls |
| `WrapPanel` | `ItemsRepeater` with `UniformGridLayout` or `WrapLayout`, or Toolkit `AutoLayout` |
| `DataGrid` | See `DataGrid` in `references/wpf-xaml.md` |
| Charting | A charting library that supports Uno Platform |

## Common mistakes

- Looking for a .NET successor to RIA Services instead of moving to an HTTP API.
- Carrying `NavigationContext.QueryString` forward through static state.
- Wrapping HTTP calls synchronously to mimic `Load()`.
- Using `System.IO.File` against hard-coded paths instead of `ApplicationData`.
- Assuming every Toolkit control has a one-to-one replacement.
