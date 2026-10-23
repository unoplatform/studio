# Uno Navigation Setup

## Key Principles (Stable)

- Add `Navigation` to `<UnoFeatures>`: `<UnoFeatures>Navigation;Toolkit</UnoFeatures>`
- Adding `Toolkit` enables TabBar and NavigationBar controls with navigation support
- Navigation is configured in App.xaml.cs: `.UseToolkitNavigation()` on the `IApplicationBuilder` before `.Configure(host => ...)`, then `.UseNavigation(RegisterRoutes)` on the host builder. With MVUX use `.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)` so `*Model` records map to their generated ViewModels. A bare `.UseNavigation()` compiles but registers no routes
- Routes are registered using `ViewMap`, `DataViewMap`, or `RouteMap`
- In an app created with `-shell`, the navigation host (`Shell.xaml`) is the root of the navigation hierarchy. Without it (the template default) there is no `Shell.xaml` or `ShellModel`; `InitializeNavigationAsync(..., initialRoute: "Main")` navigates the window straight to `"Main"`
- Do NOT use `Region.Attached="True"` inside Shell.xaml or ExtendedSplashScreen content

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Navigation Overview & Setup Documentation

```
uno_platform_docs_search("Uno Navigation Extensions setup configuration routes UseNavigation")
```

Primary documentation pages:
- **Navigation Overview**: `external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md`
- **Define Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md`
- **Register Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md`

Fetch the overview page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md", anchor="…")
```

### For Route Registration

Fetch the route registration walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md", anchor="…")
```

### For Host Builder Configuration

If the user needs the App.xaml.cs setup:

```
uno_platform_docs_search("Uno Navigation host builder UseNavigation UseToolkitNavigation App.xaml.cs")
```

## Related Skills

- `references/routes.md` — Defining and registering routes
- `references/regions.md` — Region-based navigation
- `references/code.md` — Programmatic navigation
- `references/xaml.md` — Declarative XAML navigation
