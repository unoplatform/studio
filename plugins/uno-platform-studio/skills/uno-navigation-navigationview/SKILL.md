---
name: uno-navigation-navigationview
description: "Implement NavigationView navigation in Uno Platform using region-based navigation. Provides complete, compilable shell template for sidebar/hamburger navigation. Use for >5 pages, hierarchical content, or desktop/enterprise apps."
when_to_use: "Use when more than 5 top-level pages, hierarchical or content-heavy apps, desktop / enterprise apps, prompt mentions \"dashboard\", \"admin\", \"desktop\", \"sidebar\", or \"drawer\", building a sidebar navigation pattern, or implementing a hamburger menu."
metadata:
  author: uno-platform
  version: "3.3"
  category: navigation
---

# Uno Navigation with NavigationView — Agent Skill

## Reference Templates

For complete, compilable shell XAML with parameterized placeholders, see:

**`references/shell-navigationview-template.md`** — Contains:
- Icon lookup table (Home, Search, Settings, Profile, etc.)
- SHELL_PAGE_NAME.xaml — Without Settings variant
- SHELL_PAGE_NAME.xaml — With Settings variant
- SHELL_PAGE_NAME.xaml.cs — Settings code-behind (`Region.SetName` for built-in Settings item)
- Page template (XAML + code-behind)
- Route registration template
- Substitution rules
- Localization template (Resources.resw)

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the NavigationView Navigation Documentation

```
uno_platform_docs_search("Uno Navigation NavigationView sidebar hamburger region")
```

Primary documentation pages:
- **Define Regions (NavigationView section)**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **Responsive Shell**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/ResponsiveShell.md`

Fetch the define regions page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md")
```

Focus on the "Using Regions with NavigationView" section.

### Step 2: For Responsive Navigation

If the user needs adaptive navigation (NavigationView on desktop, TabBar on mobile):

See the `uno-navigation-responsive-shell` skill.

## Key Principles (Stable)

- NavigationView uses region-based navigation to link menu items to content
- Parent container needs `uen:Region.Attached="True"`
- NavigationView needs `uen:Region.Attached="True"`
- Each `NavigationViewItem` uses `uen:Region.Name="RouteName"` to map to content
- Content area uses a Grid with `uen:Region.Attached="True"` and `uen:Region.Navigator="Visibility"`
- XAML namespace: `xmlns:uen="using:Uno.Extensions.Navigation.UI"`

## Critical Rules

- The root `Grid` with `uen:Region.Attached="True"` enables region navigation for the entire shell
- `uen:Region.Attached="True"` MUST also be on the `NavigationView` itself
- The content area `Grid` inside `NavigationView` needs BOTH `uen:Region.Attached="True"` AND `uen:Region.Navigator="Visibility"`
- **The content area Grid with `Region.Navigator="Visibility"` must be empty in XAML.** Do not add `Collapsed` child elements for each route. The navigation framework resolves registered routes and injects the corresponding views at runtime.
- The Settings item is NOT a `NavigationViewItem` in `MenuItems` — use `Loaded` event + `Region.SetName` in code-behind
- Route names in `RouteMap` MUST match the `uen:Region.Name` values in `SHELL_PAGE_NAME.xaml`
- Page routes are nested under the `"Main"` route so only the content area updates, not the entire page
- Do NOT use `Region.Attached="True"` inside `Shell.xaml` — only in `SHELL_PAGE_NAME.xaml`

## Related Skills

- [[uno-navigation-regions]] — Region concepts
- [[uno-navigation-responsive-shell]] — Adaptive NavigationView + TabBar
- [[uno-navigation-panel-visibility]] — Visibility-based content switching details
- [[uno-navigation-tabbar]] — TabBar alternative
- [[uno-navigation-routes]] — Route registration details
