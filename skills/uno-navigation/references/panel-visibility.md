# Uno Navigation Panel Visibility

This reference covers visibility-based navigation where the framework injects registered route views into an empty `Grid` and switches between them by toggling `Visibility`. This is the content-area pattern used inside TabBar, NavigationView, and responsive shell layouts.

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Visibility Navigation Documentation

```
uno_platform_docs_search("Uno Navigation visibility based panel Grid Region.Navigator content switch")
```

Primary documentation pages:
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **Navigation Region Reference**: `external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md`

Fetch the define regions walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md")
```

## Key Principles (Stable)

- Set `uen:Region.Navigator="Visibility"` on the content container Grid
- Set `uen:Region.Attached="True"` on the same Grid
- **The Grid marked with `uen:Region.Navigator="Visibility"` must be empty in XAML.** Do not add `Collapsed` child elements for each route. The navigation framework resolves registered routes and injects the corresponding views at runtime.
- After injection, children are toggled via Visibility — all remain in the visual tree
- Good for small numbers of views where you want instant switching
- No back stack — just visibility toggles

## Typical Usage Pattern

The visibility region Grid appears as the content area inside navigation shells:

```xml
<!-- Inside a TabBar or NavigationView shell -->
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility" />
```

The framework creates view instances from registered `RouteMap` entries and places them as children of this Grid, managing their `Visibility` as navigation occurs.

## Critical Rules

- The Grid MUST have both `uen:Region.Attached="True"` AND `uen:Region.Navigator="Visibility"`
- The Grid MUST be empty — do NOT pre-populate with collapsed child elements
- Route names in `RouteMap` determine which views get injected
- The parent navigation control (TabBar, NavigationView) drives which child becomes visible

## Related Skills

- `references/regions.md` — Region concepts
- `references/contentcontrol.md` — ContentControl-based switching (alternative)
- `references/navigationview.md` — NavigationView shell using visibility regions
- `references/tabbar.md` — TabBar shell using visibility regions
- `references/responsive-shell.md` — Responsive shell using visibility regions
