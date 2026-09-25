# Uno Navigation Setup

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Navigation Overview & Setup Documentation

```
uno_platform_docs_search("Uno Navigation Extensions setup configuration routes UseNavigation")
```

Primary documentation pages:
- **Navigation Overview**: `external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md`
- **Define Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md`
- **Register Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md`

Fetch the overview page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md")
```

### Step 2: For Route Registration

Fetch the route registration walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md")
```

### Step 3: For Host Builder Configuration

If the user needs the App.xaml.cs setup:

```
uno_platform_docs_search("Uno Navigation host builder UseNavigation UseToolkitNavigation App.xaml.cs")
```

## Key Principles (Stable)

- Add `Navigation` to `<UnoFeatures>`: `<UnoFeatures>Navigation;Toolkit</UnoFeatures>`
- Adding `Toolkit` enables TabBar and NavigationBar controls with navigation support
- Navigation is configured via the host builder in App.xaml.cs using `.UseNavigation()`
- Routes are registered using `ViewMap`, `DataViewMap`, or `RouteMap`
- The navigation host (`Shell.xaml`) is the root of the navigation hierarchy
- Do NOT use `Region.Attached="True"` inside Shell.xaml or ExtendedSplashScreen content

## Related Skills

- `references/routes.md` — Defining and registering routes
- `references/regions.md` — Region-based navigation
- `references/code.md` — Programmatic navigation
- `references/xaml.md` — Declarative XAML navigation
