---
name: uno-navigation-xaml
description: "Implement declarative navigation in Uno Platform XAML using Navigation.Request and Navigation.Data attached properties."
when_to_use: "Use when adding navigation to buttons, list items, or controls directly in XAML, navigation without writing code-behind, using `uen:Navigation.Request` attached property for route-based navigation, passing data via `uen:Navigation.Data` in XAML, navigation from `ItemsRepeater` or `ListView` item clicks/selections, or simple Frame shell: 1-2 pages, navigation is secondary, no TabBar or NavigationView needed."
metadata:
  author: uno-platform
  version: "3.3"
  category: navigation
---

# Uno Navigation via XAML — Agent Skill

## Reference Templates

For a complete, compilable Frame-based shell with parameterized placeholders, see:

**`references/shell-frame-template.md`** — Contains:
- SHELL_PAGE_NAME.xaml shell template
- Navigation.Request syntax reference
- Page template (XAML + code-behind)
- Route registration template
- Substitution rules

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the XAML Navigation Documentation

```
uno_platform_docs_search("Uno Navigation XAML Navigation.Request Navigation.Data attached properties declarative")
```

Primary documentation pages:
- **Navigate Using Attached Properties in XAML**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/NavigateUsingAttachedPropertiesInXAML.md`
- **XAML Navigation in Chefs**: `external/uno.chefs/doc/navigation/XamlNavigation.md`
- **Navigation Overview (XAML section)**: `external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md`

Fetch the main walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/NavigateUsingAttachedPropertiesInXAML.md")
```

### Step 2: For Chefs App Examples

For real-world XAML navigation patterns:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/navigation/XamlNavigation.md")
```

### Step 3: For Qualifiers in XAML

If the user needs back-stack manipulation in XAML (like `-/` to clear):

See the `uno-navigation-qualifiers` skill.

## Key Principles (Stable)

- XAML namespace: `xmlns:uen="using:Uno.Extensions.Navigation.UI"`
- `uen:Navigation.Request="RouteName"` triggers navigation on interaction (click/tap)
- `uen:Navigation.Data="{Binding SomeData}"` passes data with the navigation request
- Works on Button, ListView, ItemsRepeater, and other interactive controls
- No Click event handler or code-behind needed
- Supports qualifier prefixes in the route string (e.g., `-/Login` to clear back stack)

## Navigation.Request Syntax

- **Navigate forward**: `uen:Navigation.Request="PageName"` — pushes page onto the frame stack
- **Navigate and clear back stack**: `uen:Navigation.Request="-/PageName"` — replaces the current page
- **Navigate back**: `uen:Navigation.Request="!back"` — pops the current page
- **Navigate to nested region**: `uen:Navigation.Request="./RegionName"` — for visibility-based regions

## Critical Rules

- `uen:Navigation.Request` is an attached property — it works on any UI element that supports `Click` or `Tapped`
- Navigation happens via XAML only — no code-behind `INavigator` calls needed for simple transitions
- The `!back` request navigates back in the frame stack
- The `-/` prefix clears the back stack (use for login-to-home transitions)
- Route names in `RouteMap` MUST match the `uen:Navigation.Request` values used in XAML
- Do NOT use `Region.Attached="True"` inside `Shell.xaml` — only in `SHELL_PAGE_NAME.xaml`
- On secondary pages, always include a back button with `uen:Navigation.Request="!back"`

## Related Skills

- [[uno-navigation-code]] — Programmatic navigation alternative
- [[uno-navigation-qualifiers]] — Qualifier prefixes
- [[uno-navigation-routes]] — Route registration
- [[uno-navigation-data]] — Data passing patterns
- [[uno-navigation-tabbar]] — Alternative: bottom tab navigation for 3-5 pages
- [[uno-navigation-navigationview]] — Alternative: sidebar navigation for desktop
