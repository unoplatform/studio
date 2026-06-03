---
name: uno-navigation-responsive-shell
description: "Implement responsive navigation shells that adapt between TabBar and NavigationView based on screen size. Provides complete, compilable shell template with VisualStateManager breakpoints."
when_to_use: "Use when an app must work well on both mobile and desktop, upgrading a single-form-factor shell to responsive, or switching between `TabBar` on mobile and `NavigationView` on desktop via `VisualStateManager` or `ResponsiveExtension`."
metadata:
  author: uno-platform
  version: "3.5"
  category: navigation
---

# Uno Navigation Responsive Shell — Agent Skill

## Reference Templates

For a complete, compilable responsive shell XAML with parameterized placeholders, see:

**`references/shell-responsive-template.md`** — Contains:
- Icon lookup table (Home, Search, Settings, Profile, etc.)
- SHELL_PAGE_NAME.xaml responsive shell template (NavigationView + TabBar + VisualStateManager)
- Breakpoint summary table (Narrow / Normal / Wide)
- Route registration template
- Substitution rules

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Responsive Shell Documentation

```
uno_platform_docs_search("Uno Navigation responsive shell NavigationView TabBar adaptive screen size")
```

Primary documentation page:
- **Build Responsive Navigation Layouts**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/ResponsiveShell.md`

Fetch the walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/ResponsiveShell.md")
```

### Step 2: For ResponsiveExtension

If using the Toolkit's `ResponsiveExtension`:

```
uno_platform_docs_search("Uno Toolkit responsive extension screen size breakpoints")
```

See also the `uno-toolkit-responsive` skill.

## Key Principles (Stable)

- Use VisualStateManager with adaptive triggers to show/hide different navigation controls
- Both TabBar and NavigationView can use the same region names for shared content
- `ResponsiveExtension` can toggle Visibility based on breakpoints (Normal, Wide, etc.)
- The content area remains the same — only the navigation control changes
- Common pattern: TabBar at bottom for mobile, NavigationView sidebar for desktop
- XAML namespaces: `xmlns:uen="using:Uno.Extensions.Navigation.UI"` and `xmlns:utu="using:Uno.Toolkit.UI"`

## Breakpoint Summary

| State | Width | TabBar | NavigationView Pane | Use Case |
|---|---|---|---|---|
| Narrow | < 700px | Visible | Hidden | Mobile phones |
| Normal | 700-999px | Hidden | Visible (auto) | Tablets |
| Wide | >= 1000px | Hidden | Visible (expanded) | Desktops |

## Critical Rules

- Both navigation controls (`NavigationView` and `TabBar`) MUST have `uen:Region.Attached="True"`
- Both MUST use identical `uen:Region.Name` values for the same pages — they share the content area
- The `VisualStateManager` goes on the root `Grid` with `uen:Region.Attached="True"`
- The `Narrow` state has NO `StateTriggers` — it is the default (smallest) state
- States are ordered by ascending width: Narrow (default) < Normal (700px) < Wide (1000px)
- The shared content area with `Region.Navigator="Visibility"` serves both navigation controls
- **The content area Grid with `Region.Navigator="Visibility"` must be empty in XAML.** Do not add `Collapsed` child elements for each route. The navigation framework resolves registered routes and injects the corresponding views at runtime.
- Do NOT use `Region.Attached="True"` inside `Shell.xaml` — only in `SHELL_PAGE_NAME.xaml`

## Related Skills

- [[uno-navigation-tabbar]] — Mobile-only shell
- [[uno-navigation-navigationview]] — Desktop-only shell
- [[uno-navigation-panel-visibility]] — Visibility-based content switching details
- [[uno-toolkit-responsive]] — Responsive markup extensions
- [[uno-navigation-regions]] — Region concepts
