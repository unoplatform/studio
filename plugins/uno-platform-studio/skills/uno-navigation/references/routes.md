# Uno Navigation Routes

## Key Principles (Stable)

- Routes are registered in `App.xaml.cs` via the `RegisterRoutes` method
- `ViewMap` — associates a View with a ViewModel
- `DataViewMap` — associates a View/ViewModel with a data type for data-based navigation
- `ResultDataViewMap` — like DataViewMap but also defines a result type
- `RouteMap` — defines a named route with nested child routes
- Route names typically match the page name without the "Page" suffix

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Routes Documentation

```
uno_platform_docs_search("Uno Navigation routes ViewMap DataViewMap RouteMap register")
```

Primary documentation pages:
- **Define Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md`
- **Register Routes**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md`

Fetch the define routes page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRoutes.md", anchor="…")
```

### For View-ViewModel Association

Fetch the register routes page for binding views to view models:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/RegisterRoutes.md", anchor="…")
```

### For Data-Based and Result-Based Navigation

If the user needs to pass data or get results via routes:

```
uno_platform_docs_search("Uno Navigation DataViewMap ResultDataViewMap data navigation result")
```

See also the `references/data.md`.

## Related Skills

- `references/setup.md` — Initial navigation configuration
- `references/data.md` — Passing data during navigation
- `references/code.md` — Programmatic navigation using routes
- `references/xaml.md` — XAML-based navigation using route names
