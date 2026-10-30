---
type: llm
---
The reply explains how to add an Uno Toolkit TabBar with Uno.Extensions navigation to an existing Uno Platform app created with `-preset blank` (single project, Uno.Sdk, Fluent styles, no Material or Simple theme).
PASS if all hold:
- It adds `Toolkit` and `Navigation` to the `<UnoFeatures>` property in the app's .csproj (the Uno SDK brings in the packages and versions), rather than hand-adding `PackageReference`s for `Uno.Toolkit.*` / `Uno.Extensions.*`.
- It wires navigation in App.xaml.cs: the host builder with `.UseNavigation(...)` registering the routes (`ViewMap`/`RouteMap`), and `.UseToolkitNavigation()` for the TabBar integration.
- The TabBar XAML uses `xmlns:utu="using:Uno.Toolkit.UI"` and `xmlns:uen="using:Uno.Extensions.Navigation.UI"`, `uen:Region.Attached`, and a `uen:Region.Name` per tab that matches a route.
- It does not rely on theme keys the blank preset lacks: either it omits `Style="{StaticResource BottomTabBarStyle}"`/`BottomTabBarItemStyle`/`MaterialBottomTabBarStyle` and `BackgroundBrush`-style Material/Simple resources, or it explicitly adds a theme (`Material`/`SimpleTheme` in `UnoFeatures` plus the theme resources in App.xaml) or warns that those keys need such a theme.
FAIL if the main instruction is to install NuGet packages directly instead of `UnoFeatures`, or if it uses Material/Simple-only style keys or brushes on the blank (Fluent) app without adding or mentioning the theme.
