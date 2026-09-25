---
name: uno-navigation
description: "Uno.Extensions.Navigation for Uno Platform apps: route registration with ViewMap, DataViewMap, ResultDataViewMap, and RouteMap; region-based navigation with Region.Attached, Region.Name, and Region.Navigator; INavigator code navigation (NavigateViewModelAsync, NavigateBackAsync, *ForResultAsync); declarative uen:Navigation.Request and Navigation.Data in XAML; dialogs, flyouts, and message prompts; back-stack qualifiers (-/ and !/); TabBar, NavigationView, responsive, and Frame shell templates; ContentControl and visibility regions; troubleshooting. Use whenever an Uno app needs a new page, a details or settings screen, a 'go to' or 'open' action, a back button, tabs, a sidebar, hamburger menu, drawer, app shell, modal, popup, confirmation dialog, deep link, login-to-home flow, or must pass an item or ID to another page, even if the user never says 'navigation'. Read this before adding any Page or wiring a Click that changes the screen."
metadata:
  author: uno-platform
  version: "3.0"
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
| Add navigation to a project, configure the host, understand Shell.xaml | `references/setup.md` | `<UnoFeatures>Navigation;Toolkit</UnoFeatures>`, `.UseNavigation()`, `RegisterRoutes` |
| Register pages and ViewModels, nest routes | `references/routes.md` | `ViewMap`, `DataViewMap`, `ResultDataViewMap`, `RouteMap` |
| Navigate from a Button, list item, or any control without code-behind | `references/xaml.md` (+ `references/shell-frame-template.md`) | `uen:Navigation.Request`, `!back`, `-/Route`, `./Region` |
| Navigate from a ViewModel, code-behind, or service | `references/code.md` | `INavigator`, `NavigateViewModelAsync`, `NavigateRouteAsync`, `NavigateBackAsync`, `*ForResultAsync` |
| Pass an entity or ID to a details page, return a result | `references/data.md` | `DataViewMap`, `NavigateDataAsync`, `uen:Navigation.Data`, `NavigateBackWithResultAsync` |
| Alert, confirmation, modal, or flyout | `references/dialogs.md` | `ShowMessageDialogAsync`, `!/` or `Qualifiers.Dialog`, `ContentDialog` vs `Page` |
| Clear the back stack (after login), open dialogs by prefix | `references/qualifiers.md` | `-/`, `!/`, `Qualifiers.ClearBackStack`, `Qualifiers.Dialog` |
| Link a navigation control to a content area, nested regions | `references/regions.md` | `uen:Region.Attached`, `uen:Region.Name`, `uen:Region.Navigator` |
| Bottom-tab shell | `references/tabbar.md` (+ `references/shell-tabbar-template.md`) | `utu:TabBar`, `TabBarItem` + `Region.Name`, `BottomTabBarStyle` |
| Sidebar/hamburger shell, Settings item | `references/navigationview.md` (+ `references/shell-navigationview-template.md`) | `NavigationView` + `Region.Attached`, `NavigationViewItem` + `Region.Name`, `Region.SetName` |
| One shell for phone and desktop | `references/responsive-shell.md` (+ `references/shell-responsive-template.md`) | `VisualStateManager` breakpoints, shared `Region.Name`, `ResponsiveExtension` |
| Content area that swaps views by visibility (inside shells) | `references/panel-visibility.md` | empty `Grid` with `Region.Attached` + `Region.Navigator="Visibility"` |
| Swap content in one pane with no back stack | `references/contentcontrol.md` | `ContentControl` + `Region.Attached` |
| Blank page, route not found, back not working, data not arriving | `references/troubleshooting.md` | checklist of the rules below |

## Critical rules

- **Every page is registered.** Each View/ViewModel pair needs a `ViewMap` (or `DataViewMap`/`ResultDataViewMap`) and a `RouteMap` entry in `RegisterRoutes`; an unregistered route yields a blank page or a runtime error. Route names usually match the page name without the `Page` suffix.
- **Route names must match in three places.** The `RouteMap` name, the `uen:Region.Name` on the tab or menu item, and any `uen:Navigation.Request` string are compared literally. Nest page routes under the `"Main"` shell route so only the content area changes.
- **Never put `Region.Attached="True"` in `Shell.xaml` or ExtendedSplashScreen content.** The navigation host is not ready there. Regions belong in the shell *page* (`SHELL_PAGE_NAME.xaml`) that the host navigates to.
- **The content area is an empty `Grid`** with both `uen:Region.Attached="True"` and `uen:Region.Navigator="Visibility"`. Do not pre-populate it with collapsed pages; the framework injects registered views at runtime and toggles their visibility.
- **The navigation control and the content area share a parent that has `Region.Attached="True"`, and the control itself also has `Region.Attached="True"`.** TabBar and NavigationView both need it, and in a responsive shell both use identical `Region.Name` values because they drive the same content area.
- **Prefer `uen:Navigation.Request` in XAML** over code-behind for simple transitions; it works on any element with `Click` or `Tapped`. Secondary pages get a back button with `uen:Navigation.Request="!back"`.
- **Navigation methods are extension methods on `INavigator`.** Get one via constructor injection or `this.GetNavigator()`, then `await navigator.NavigateViewModelAsync<T>(this)`. Calling `NavigateRouteAsync` on a string or a view is a compile error (CS1929). Always `await`.
- **Pick the result overload when a value comes back.** `NavigateViewModelForResultAsync<TViewModel, TResult>` plus `NavigateBackWithResultAsync(data)` on the destination; the plain overload silently discards the result. Register the pair with `ResultDataViewMap`.
- **Data arrives through the ViewModel constructor.** Register a `DataViewMap<TView, TViewModel, TData>` and pass the entity with `NavigateDataAsync` or `uen:Navigation.Data="{Binding Item}"`; the framework resolves the destination from the data type.
- **Use qualifiers for back-stack intent.** `-/Home` (or `Qualifiers.ClearBackStack`) after login so back cannot return to the login page; `!/` (or `Qualifiers.Dialog`) opens a route as a dialog: a `Page` target shows as a flyout, a `ContentDialog` target as a modal. Simple prompts need no route at all: `navigator.ShowMessageDialogAsync(...)`.
- **The NavigationView Settings item is not a `NavigationViewItem`.** Wire the built-in item in code-behind on `Loaded` with `Region.SetName`; the template shows how.
- **Namespaces:** `xmlns:uen="using:Uno.Extensions.Navigation.UI"` for regions and attached properties, `xmlns:utu="using:Uno.Toolkit.UI"` for `TabBar`. `Toolkit` must be in `<UnoFeatures>` alongside `Navigation` for TabBar and NavigationBar.

## Related skills

- `uno-mvux` for the Models that pages bind to; MVUX `*Model` records are what `ViewMap`/`DataViewMap` register as the ViewModel type.
- `uno-toolkit` for the controls used inside shells: `references/tabbar.md` (TabBar styling, badges, orientations), `references/navigationbar.md` (page app bar with native back button), `references/responsive.md` (breakpoint markup extension).
- `uno-themes` for the styles a shell picks up (`BottomTabBarStyle`, `MaterialBottomTabBarItemStyle`) and for restyling navigation chrome.
