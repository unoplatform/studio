---
type: llm
---
The reply adds a Customers page to an Uno Platform app that already uses CommunityToolkit.Mvvm.
PASS if the page XAML is WinUI 3 (`Microsoft.UI.Xaml` types, `x:Bind` or `{Binding}` to the view model).
FAIL if it uses WPF, Xamarin.Forms or .NET MAUI XAML syntax or types.
