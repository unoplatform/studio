---
name: uno-navigation
description: "Uno.Extensions.Navigation for Uno Platform apps: route registration with ViewMap, DataViewMap, ResultDataViewMap, and RouteMap; region-based navigation with Region.Attached, Region.Name, and Region.Navigator; INavigator code navigation (NavigateViewModelAsync, NavigateBackAsync, *ForResultAsync); declarative uen:Navigation.Request and Navigation.Data in XAML; dialogs, flyouts, and message prompts; back-stack qualifiers (-/ and !/); TabBar, NavigationView, responsive, and Frame shell templates; ContentControl and visibility regions; troubleshooting. Use whenever an Uno app needs a new page, a details or settings screen, a 'go to' or 'open' action, a back button, tabs, a sidebar, hamburger menu, drawer, app shell, modal, popup, confirmation dialog, deep link, login-to-home flow, or must pass an item or ID to another page, even if the user never says 'navigation'. Read this before adding any Page or wiring a Click that changes the screen."
metadata:
  author: uno-platform
  category: navigation
---

# Uno Navigation

Uno.Extensions.Navigation is route-based: pages and ViewModels are registered once in `App.xaml.cs`, then navigation happens by route name from XAML (`uen:Navigation.Request`) or code (`INavigator`). Shells (TabBar, NavigationView, responsive) are built from *regions*, where the framework injects registered views into an empty container at runtime. Generic agents reliably get this wrong: they hand-wire `Frame.Navigate`, pre-populate the content area with collapsed pages, put `Region.Attached` in `Shell.xaml`, or call navigation methods on the wrong type. This skill stops those mistakes and routes you to the reference for the task.

## Workflow

1. Pick a shell (below) if the app has no navigation yet, then find each task in the topic map and read the matching `references/*.md` before writing code. Shell references point to complete, compilable templates with placeholders; copy those rather than composing shell XAML from memory.
2. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path.
3. Check the generated XAML and route registration against the critical rules below before finishing. Most navigation bugs are a mismatch between a route name and a `Region.Name`/`Navigation.Request` value, or a container that is not empty.

## Choose a shell

- **TabBar** (bottom tabs): 3 to 5 top-level pages, mobile-first or consumer apps. The default when the prompt does not say desktop or enterprise. `references/tabbar.md`.
- **NavigationView** (sidebar/hamburger): more than 5 pages, hierarchical content, desktop, admin, dashboard, or enterprise apps. `references/navigationview.md`.
- **Responsive**: must work well on both phone and desktop; TabBar under 700px, NavigationView above. `references/responsive-shell.md`.
- **Frame** (linear): 1 or 2 pages, navigation is secondary, no tabs or sidebar. Buttons with `uen:Navigation.Request`. `references/xaml.md`.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Add navigation to a project, configure the host, understand Shell.xaml | `references/setup.md` | `<UnoFeatures>Navigation;Toolkit</UnoFeatures>`, `.UseNavigation(RegisterRoutes)`, `.UseToolkitNavigation()` |
| Register pages and ViewModels, nest routes | `references/routes.md` | `ViewMap`, `DataViewMap`, `ResultDataViewMap`, `RouteMap` |
| Navigate from a Button, list item, or any control without code-behind | `references/xaml.md` (+ `references/shell-frame-template.md`) | `uen:Navigation.Request`, `-` (back), `-/Route`, `./Region` |
| Navigate from a ViewModel, code-behind, or service | `references/code.md` | `INavigator`, `this.Navigator()`, `NavigateViewModelAsync<T>(this)`, `NavigateRouteAsync(this, "Route")`, `NavigateBackAsync(this)`, `*ForResultAsync` |
| Pass an entity or ID to a details page, return a result | `references/data.md` | `DataViewMap`, `NavigateDataAsync`, `uen:Navigation.Data`, `NavigateBackWithResultAsync` |
| Alert, confirmation, modal, or flyout | `references/dialogs.md` | `ShowMessageDialogAsync`, `!Route` or `Qualifiers.Dialog`, `ContentDialog` vs `Page` |
| Clear the back stack (after login), drop the current page, open dialogs by prefix | `references/qualifiers.md` | `-/Route`, `-Route`, `!Route`, `Qualifiers.ClearBackStack`, `Qualifiers.Dialog` |
| Link a navigation control to a content area, nested regions | `references/regions.md` | `uen:Region.Attached`, `uen:Region.Name`, `uen:Region.Navigator` |
| Bottom-tab shell | `references/tabbar.md` (+ `references/shell-tabbar-template.md`) | `utu:TabBar`, `TabBarItem` + `Region.Name`, `BottomTabBarStyle`, `BottomTabBarItemStyle` |
| Sidebar/hamburger shell, Settings item | `references/navigationview.md` (+ `references/shell-navigationview-template.md`) | `NavigationView` + `Region.Attached`, `NavigationViewItem` + `Region.Name`, Settings in `FooterMenuItems` |
| One shell for phone and desktop | `references/responsive-shell.md` (+ `references/shell-responsive-template.md`) | `VisualStateManager` breakpoints, shared `Region.Name`, `ResponsiveExtension` |
| Content area that swaps views by visibility (inside shells) | `references/panel-visibility.md` | empty `Grid` with `Region.Attached` + `Region.Navigator="Visibility"` |
| Swap content in one pane with no back stack | `references/contentcontrol.md` | `ContentControl` + `Region.Attached` |
| Blank page, route not found, back not working, data not arriving | `references/troubleshooting.md` | checklist of the rules below |

## Critical rules

- **Every page is registered.** Each View/ViewModel pair needs a `ViewMap` (or `DataViewMap`/`ResultDataViewMap`) and a `RouteMap` entry in `RegisterRoutes`; an unregistered route yields a blank page or a runtime error. Route names usually match the page name without the `Page` suffix.
- **Route names must match in three places.** The `RouteMap` name, the `uen:Region.Name` on the tab or menu item, and any `uen:Navigation.Request` string are compared literally. In TabBar and NavigationView shells, nest page routes under the `"Main"` shell route so only the content area changes; in a plain Frame shell, register pages as siblings of `"Main"` under the `""` root, as the template does.
- **Never put `Region.Attached="True"` in `Shell.xaml` or ExtendedSplashScreen content.** The navigation host is not ready there. Regions belong in the shell *page* (`SHELL_PAGE_NAME.xaml`) that the host navigates to.
- **The content area is a `Grid`** with both `uen:Region.Attached="True"` and `uen:Region.Navigator="Visibility"`. Keep it empty (the framework injects registered views at runtime and toggles their visibility), or give every pre-placed child a `uen:Region.Name` matching its route, as the docs' responsive walkthrough does. Never drop unnamed collapsed pages in it.
- **The navigation control and the content area share a parent that has `Region.Attached="True"`, and the control itself also has `Region.Attached="True"`.** TabBar and NavigationView both need it, and in a responsive shell both use identical `Region.Name` values because they drive the same content area.
- **Prefer `uen:Navigation.Request` in XAML** over code-behind for simple transitions; it works on any element with `Click` or `Tapped`. Secondary pages get a back button with `uen:Navigation.Request="-"`.
- **Navigation methods are extension methods on `INavigator`, and every one takes `sender` first.** Get the navigator via constructor injection or `this.Navigator()` on a view (nullable), then `await navigator.NavigateViewModelAsync<T>(this)`, `NavigateRouteAsync(this, "Route")`, `NavigateBackAsync(this)`. Calling `NavigateRouteAsync` on a string or a view is a compile error (CS1929, or CS1061 if `Uno.Extensions.Navigation` is not imported). `Region.GetNavigator(element)` is unrelated: it returns the `Region.Navigator` string. Always `await`.
- **Pick the result overload when a value comes back.** `NavigateViewModelForResultAsync<TViewModel, TResult>(this)` plus `NavigateBackWithResultAsync(this, data: value)` on the destination; the plain overload silently discards the result. The response's `.Result` is a `Task<Option<TResult>>`, so `await response!.Result` and test `IsSome`. Register the pair with `ResultDataViewMap`.
- **Data arrives through the ViewModel constructor.** Register a `DataViewMap<TView, TViewModel, TData>` and pass the entity with `NavigateDataAsync` or `uen:Navigation.Data="{Binding Item}"`; the framework resolves the destination from the data type.
- **Use qualifiers for back-stack intent.** `-/Home` (or `Qualifiers.ClearBackStack`) after login clears the whole back stack so back cannot return to the login page; `-Home` drops only the current page; `!Route` (or `Qualifiers.Dialog`) opens a route as a dialog: a `Page` target shows as a flyout, a `ContentDialog` target as a modal (a `ContentDialog` route opens as a modal even without `!`). Simple prompts need no route at all: `ShowMessageDialogAsync<string>(this, title:, content:, buttons:)` returns the chosen `DialogAction.Id` (or its `Label` when `Id` is null); the non-generic overload returns plain `Task`.
- **Do not use the built-in NavigationView Settings item.** `NavigationViewNavigator` only enumerates `MenuItems` and `FooterMenuItems`, so a `Region.SetName` on `SettingsItem` navigates but never syncs selection: navigating to Settings from code or a deep link leaves the previous item highlighted. Set `IsSettingsVisible="False"` and add a normal `NavigationViewItem` with `uen:Region.Name="Settings"` to `NavigationView.FooterMenuItems`.
- **Namespaces:** `xmlns:uen="using:Uno.Extensions.Navigation.UI"` for regions and attached properties, `xmlns:utu="using:Uno.Toolkit.UI"` for `TabBar`. `Toolkit` must be in `<UnoFeatures>` alongside `Navigation` for TabBar and NavigationBar.
- **Host setup registers the routes.** `.UseToolkitNavigation()` on the `IApplicationBuilder`, then `.UseNavigation(RegisterRoutes)` on the host, or `.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)` with MVUX. A bare `.UseNavigation()` compiles and registers nothing.
- **Tab items use the alias style keys.** `BottomTabBarStyle` on the `TabBar`, `BottomTabBarItemStyle` on each `TabBarItem`. Only the Material and Simple toolkit themes define these aliases; `MaterialBottomTabBarItemStyle` exists only in Material and throws under Simple, the template default. On Fluent (the `-preset blank` default) omit the `Style` attributes; on Cupertino use `CupertinoBottomTabBarStyle`/`CupertinoBottomTabBarItemStyle`. The same applies to the page background: `BackgroundBrush` exists only in Uno.Themes, so on Fluent use `ApplicationPageBackgroundThemeBrush` and on Cupertino `CupertinoSystemBackgroundBrush`, as the template does.

## Related skills

- `uno-mvux` for the Models that pages bind to; MVUX `*Model` records are what `ViewMap`/`DataViewMap` register as the ViewModel type.
- `uno-toolkit` for the controls used inside shells: `references/tabbar.md` (TabBar styling, badges, orientations), `references/navigationbar.md` (page app bar with native back button), `references/responsive.md` (breakpoint markup extension).
- `uno-toolkit` also owns the shell style keys (`BottomTabBarStyle`, `BottomTabBarItemStyle`; see `references/tabbar.md` and `references/material-theme.md`); `uno-themes` for restyling navigation chrome with semantic brushes.
