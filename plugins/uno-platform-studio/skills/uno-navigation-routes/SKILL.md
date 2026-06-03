---
name: uno-navigation-routes
description: "Define and register navigation routes in Uno Platform applications using ViewMap, DataViewMap, ResultDataViewMap, and RouteMap."
when_to_use: "Use when registering views and ViewModels for navigation, setting up `ViewMap`, `DataViewMap`, or `ResultDataViewMap`, configuring nested routes and route hierarchies, adding route dependencies or initialization logic, or understanding how routes map to navigation paths."
metadata:
  author: uno-platform
  version: "2.3"
  category: navigation
---

# Uno Navigation Routes — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Routes Documentation

```
uno_platform_docs_search("Uno Navigation routes ViewMap DataViewMap RouteMap register")
```

Primary documentation pages:
- **Define Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md`
- **Register Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md`

Fetch the define routes page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md")
```

### Step 2: For View-ViewModel Association

Fetch the register routes page for binding views to view models:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md")
```

### Step 3: For Data-Based and Result-Based Navigation

If the user needs to pass data or get results via routes:

```
uno_platform_docs_search("Uno Navigation DataViewMap ResultDataViewMap data navigation result")
```

See also the `uno-navigation-data` skill.

## Key Principles (Stable)

- Routes are registered in `App.xaml.cs` via the `RegisterRoutes` method
- `ViewMap` — associates a View with a ViewModel
- `DataViewMap` — associates a View/ViewModel with a data type for data-based navigation
- `ResultDataViewMap` — like DataViewMap but also defines a result type
- `RouteMap` — defines a named route with nested child routes
- Route names typically match the page name without the "Page" suffix

## Related Skills

- [[uno-navigation-setup]] — Initial navigation configuration
- [[uno-navigation-data]] — Passing data during navigation
- [[uno-navigation-code]] — Programmatic navigation using routes
- [[uno-navigation-xaml]] — XAML-based navigation using route names
