---
name: uno-navigation-tabbar
description: "Implement TabBar navigation in Uno Platform using region-based navigation. Provides complete, compilable shell template for bottom tab navigation. Use for 3-5 top-level pages in mobile-first apps."
when_to_use: "Use when 3-5 top-level pages, mobile-first / consumer apps, default choice when prompt does not indicate desktop or enterprise, building bottom navigation (mobile), linking TabBar items to content pages via regions, or using TabBar with Navigation Extensions."
metadata:
  author: uno-platform
  version: "3.5"
  category: navigation
---

# Uno Navigation with TabBar — Agent Skill

## Reference Templates

For complete, compilable shell XAML with parameterized placeholders, see:

**`references/shell-tabbar-template.md`** — Contains:
- Icon lookup table (Home, Search, Settings, Profile, etc.)
- SHELL_PAGE_NAME.xaml shell template with TabBar
- Page template (XAML + code-behind)
- Route registration template
- Substitution rules
- Localization template (Resources.resw)

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the TabBar Navigation Documentation

```
uno_platform_docs_search("Uno Navigation TabBar bottom tab region navigation")
```

Primary documentation pages:
- **TabBar Navigation (Chefs)**: `external/uno.chefs/doc/toolkit/NavigateTabBar.md`
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`

Fetch the Chefs TabBar page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/toolkit/NavigateTabBar.md")
```

### Step 2: For TabBar Control Reference

For TabBar control properties and styling:

```
uno_platform_docs_search("Uno Toolkit TabBar TabBarItem styles badges")
```

See also the `uno-toolkit-tabbar` skill for the control itself.

### Step 3: For Responsive Navigation

If combining TabBar (mobile) with NavigationView (desktop):

See the `uno-navigation-responsive-shell` skill.

## Key Principles (Stable)

- TabBar uses region-based navigation similarly to NavigationView
- Each `TabBarItem` gets `uen:Region.Name="RouteName"`
- Parent container and content area need `Region.Attached="True"`
- Apply `BottomTabBarStyle` to the `TabBar`; per-`TabBarItem` styling comes from the active design-theme (e.g., `MaterialBottomTabBarItemStyle` from [[uno-toolkit-material-theme]])
- Requires `Toolkit` in `<UnoFeatures>` along with `Navigation`
- XAML namespaces: `xmlns:uen="using:Uno.Extensions.Navigation.UI"` and `xmlns:utu="using:Uno.Toolkit.UI"`

## Critical Rules

- The root `Grid` with `uen:Region.Attached="True"` MUST be a direct child of the Page content — it enables region navigation
- `uen:Region.Navigator="Visibility"` MUST be on the content area Grid — this tells the framework to toggle child visibility
- **The content area Grid with `Region.Navigator="Visibility"` must be empty in XAML.** Do not add `Collapsed` child elements for each route. The navigation framework resolves registered routes and injects the corresponding views at runtime.
- `uen:Region.Attached="True"` MUST also be on the TabBar itself
- Route names in `RouteMap` MUST match the `uen:Region.Name` values in `SHELL_PAGE_NAME.xaml`
- Tab page routes are nested under the `"Main"` route so only the content area updates, not the entire page
- Do NOT use `Region.Attached="True"` inside `Shell.xaml` — only in `SHELL_PAGE_NAME.xaml`

## Related Skills

- [[uno-navigation-regions]] — Region concepts
- [[uno-navigation-responsive-shell]] — Adaptive navigation
- [[uno-navigation-panel-visibility]] — Visibility-based content switching details
- [[uno-toolkit-tabbar]] — TabBar control reference
- [[uno-navigation-navigationview]] — Sidebar navigation alternative
- [[uno-navigation-routes]] — Route registration details
