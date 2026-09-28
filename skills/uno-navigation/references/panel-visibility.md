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
- **The Grid marked with `uen:Region.Navigator="Visibility"` is empty or holds named children.** Keep it empty (the framework resolves registered routes and injects views at runtime), or give every pre-placed child a `uen:Region.Name` matching its route; the navigator looks for an existing child by name before creating one. Never add unnamed `Collapsed` pages.
- After injection, children are toggled via Visibility — all remain in the visual tree
- Good for small numbers of views where you want instant switching
- Switching between routes is a visibility toggle, not a push. A route whose view is a `Page` is wrapped in a `FrameView`, so `-` still pops within that tab
- The region control must be a `Grid`; on any other panel the request bubbles up to the parent region, so the shell Frame navigates the whole page instead of the content area

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
- Keep the Grid empty, or name every pre-placed child with `uen:Region.Name`; never add unnamed collapsed children
- Route names in `RouteMap` determine which views get injected
- The parent navigation control (TabBar, NavigationView) drives which child becomes visible

## Related Skills

- `references/regions.md` — Region concepts
- `references/contentcontrol.md` — ContentControl-based switching (alternative)
- `references/navigationview.md` — NavigationView shell using visibility regions
- `references/tabbar.md` — TabBar shell using visibility regions
- `references/responsive-shell.md` — Responsive shell using visibility regions
