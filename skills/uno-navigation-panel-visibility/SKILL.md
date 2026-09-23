---
name: uno-navigation-panel-visibility
description: "Implement visibility-based navigation using Panel or Grid controls."
when_to_use: "Use when implementing lightweight content switching without `Frame` overhead via `Region.Navigator=\"Visibility\"`, when serving as the content area inside `TabBar`, `NavigationView`, or responsive shells, or when all registered views should stay in the visual tree after injection."
metadata:
  author: uno-platform
  version: "3.5"
  category: navigation
---

# Uno Navigation Panel Visibility — Agent Skill

This skill covers visibility-based navigation, where a `Grid` marked `uen:Region.Navigator="Visibility"` switches between views by toggling `Visibility`. The docs define two ways to supply those views: the framework injects registered route views into an **empty** Grid (the content-area pattern inside TabBar, NavigationView, and responsive shell layouts), or the page declares them **inline** as named child elements (in-page panes such as tabs on a detail page).

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Visibility Navigation Documentation

```
uno_platform_docs_search("Uno Navigation visibility based panel Grid Region.Navigator content switch")
```

Primary documentation pages:
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md` (covers both modes: inline content and "Method 2: Route-Based Content")
- **Use Panel**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/UsePanel.md` (inline content mode, end to end)
- **Navigation Region Reference**: `external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md`

Fetch the define regions walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md")
```

## Key Principles (Stable)

- Set `uen:Region.Navigator="Visibility"` on the content container Grid
- Set `uen:Region.Attached="True"` on the same Grid
- Choose **one** of the two documented modes per region:

| Mode | Grid content | Routes | Use for |
|---|---|---|---|
| **Route-based** | Empty in XAML; the framework injects a view per registered route | Nested `RouteMap`s under the page route, one with `IsDefault: true` | App-level tabs or menu items where each target is its own page and model (TabBar / NavigationView / responsive shells) |
| **Inline content** | One child per region, each with `uen:Region.Name` and `Visibility="Collapsed"` | None required | Panes that belong to the current page and share its DataContext (for example Ingredients / Steps / Reviews tabs on a detail page) |

- Children are toggled via Visibility — all remain in the visual tree
- Good for small numbers of views where you want instant switching
- No back stack — just visibility toggles

## Typical Usage Pattern

**Route-based** — the content area inside navigation shells:

```xml
<!-- Inside a TabBar or NavigationView shell -->
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility" />
```

The framework creates view instances from registered `RouteMap` entries and places them as children of this Grid, managing their `Visibility` as navigation occurs.

**Inline content** — panes declared in the page (from the UsePanel / DefineRegions walkthroughs):

```xml
<Grid uen:Region.Attached="True">
    <utu:TabBar uen:Region.Attached="True">
        <utu:TabBarItem Content="Ingredients" uen:Region.Name="Ingredients" />
        <utu:TabBarItem Content="Steps" uen:Region.Name="Steps" />
    </utu:TabBar>
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="Ingredients" Visibility="Collapsed">
            <!-- Ingredients content -->
        </Grid>
        <Grid uen:Region.Name="Steps" Visibility="Collapsed">
            <!-- Steps content -->
        </Grid>
    </Grid>
</Grid>
```

Buttons can target inline regions with a `./` prefix: `uen:Navigation.Request="./Steps"`.

## Critical Rules

- The Grid MUST have both `uen:Region.Attached="True"` AND `uen:Region.Navigator="Visibility"`
- Do NOT mix modes in one region: in route-based mode the Grid MUST be empty (do NOT pre-populate it with collapsed children for the routes); in inline mode every child MUST carry `uen:Region.Name` and start `Collapsed`
- Route-based mode: route names in `RouteMap` determine which views get injected
- The parent navigation control (TabBar, NavigationView) drives which child becomes visible
- Do NOT replace either mode with a `SelectionChanged` / `Click` handler that sets `Visibility` in code-behind; that bypasses the region and loses navigation state

## Related Skills

- [[uno-navigation-regions]] — Region concepts
- [[uno-navigation-contentcontrol]] — ContentControl-based switching (alternative)
- [[uno-navigation-navigationview]] — NavigationView shell using visibility regions
- [[uno-navigation-tabbar]] — TabBar shell using visibility regions
- [[uno-navigation-responsive-shell]] — Responsive shell using visibility regions
