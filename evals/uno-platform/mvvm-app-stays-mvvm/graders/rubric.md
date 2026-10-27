---
type: llm
---
The reply adds a Customers page to an Uno Platform app that already uses CommunityToolkit.Mvvm.
PASS if all hold:
- The view model follows the app's existing MVVM pattern: a `partial class` deriving from `ObservableObject` with `[ObservableProperty]`/`ObservableCollection` (or equivalent observable properties) and a `[RelayCommand]`/`AsyncRelayCommand` for refresh that calls `ICustomerService.GetCustomersAsync()`.
- It does not convert the page to MVUX (no `partial record ...Model`, `IFeed`/`IListFeed`/`IState`, or `FeedView`) and does not suggest migrating the app.
- The XAML is WinUI 3 (`Microsoft.UI.Xaml`, `x:Bind` or `{Binding}` to the view model), not WPF/Xamarin.Forms/MAUI syntax.
FAIL if it introduces MVUX or another presentation framework, or uses non-WinUI XAML.
