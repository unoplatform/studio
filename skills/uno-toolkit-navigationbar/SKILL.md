---
name: uno-toolkit-navigationbar
description: "Page-level app bar with title, back button, and action items; renders natively on iOS and Android."
when_to_use: "Use when adding a page-level app bar with title, back button (`MainCommand`), and `AppBarButton` actions in `PrimaryCommands` / `SecondaryCommands`. Renders as XAML on Windows and as the native bar on iOS / Android."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit NavigationBar — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the NavigationBar Documentation

```
uno_platform_docs_search("Uno Toolkit NavigationBar app bar MainCommand PrimaryCommands")
```

Primary documentation page:
- **NavigationBar**: `external/uno.toolkit.ui/doc/controls/NavigationBar.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/NavigationBar.md")
```

### Step 2: For Chefs Examples

```
uno_platform_docs_search("NavigationBar Chefs app example navigation back")
```

## Critical Rules

- **`MainCommand` is an `AppBarButton`** — there is no dedicated `NavigationBarMainCommand` type. Use `<utu:NavigationBar.MainCommand><AppBarButton Icon="Back" Command="{Binding GoBackCommand}"/></utu:NavigationBar.MainCommand>` (or equivalent attribute form). The same `AppBarButton` type is used for `PrimaryCommands` and `SecondaryCommands` items.

## Key Principles (Stable)

- `MainCommand` — the back-button slot; an `AppBarButton` instance
- `PrimaryCommands` — action `AppBarButton`s displayed on the right
- `SecondaryCommands` — overflow `AppBarButton`s in the overflow menu
- Two rendering modes: `Windows` (XAML drawn) and `Native` (platform AppBar on iOS/Android)
- Content property is the title
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-safearea]] — Safe area insets with NavigationBar
- [[uno-navigation-code]] — Navigation integration
