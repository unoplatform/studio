---
name: uno-navigation-setup
description: "Set up and configure Uno Platform Navigation Extensions in an application."
when_to_use: "Use when creating a new Uno Platform project with navigation support, adding navigation to an existing Uno Platform app, configuring `UseNavigation` in App.xaml.cs host builder, setting up route registration, or understanding the relationship between Navigation and Toolkit packages."
metadata:
  author: uno-platform
  version: "2.3"
  category: navigation
---

# Uno Navigation Setup — Agent Skill

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

- [[uno-navigation-routes]] — Defining and registering routes
- [[uno-navigation-regions]] — Region-based navigation
- [[uno-navigation-code]] — Programmatic navigation
- [[uno-navigation-xaml]] — Declarative XAML navigation
