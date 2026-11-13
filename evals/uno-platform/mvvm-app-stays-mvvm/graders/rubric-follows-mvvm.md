---
type: llm
---
The reply adds a Customers page to an Uno Platform app that already uses CommunityToolkit.Mvvm.
PASS if the view model follows the app's CommunityToolkit.Mvvm pattern: a `partial class` deriving from `ObservableObject` with observable properties (`[ObservableProperty]`, an `ObservableCollection`, or equivalent) and a `[RelayCommand]`/`AsyncRelayCommand` refresh that calls `ICustomerService.GetCustomersAsync()`.
FAIL if the view model is not an `ObservableObject`, or refresh does not go through a relay command that calls the service.
