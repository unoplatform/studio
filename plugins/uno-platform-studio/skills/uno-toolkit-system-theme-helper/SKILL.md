---
name: uno-toolkit-system-theme-helper
description: "Use SystemThemeHelper to detect system theme (light/dark), check if dark mode is active, and set application theme. Provides static methods for theme retrieval and management."
when_to_use: "Use when reading or programmatically switching dark / light mode from C# (not the XAML `ThemeResource` lookup path) — detecting the current system theme, setting the application theme at runtime, or toggling between modes."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit SystemThemeHelper — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the SystemThemeHelper Documentation

```
uno_platform_docs_search("Uno Toolkit SystemThemeHelper theme light dark mode detect")
```

Primary documentation page:
- **SystemThemeHelper**: `external/uno.toolkit.ui/doc/helpers/SystemThemeHelper.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/SystemThemeHelper.md")
```

## Key Principles (Stable)

- `SystemThemeHelper.GetCurrentOsTheme()` — returns `ApplicationTheme.Light` or `ApplicationTheme.Dark`
- `SystemThemeHelper.GetRootTheme(XamlRoot?)` — gets the app theme for a given XamlRoot
- `SystemThemeHelper.IsRootInDarkMode(XamlRoot?)` — quick check if dark mode is active
- `SystemThemeHelper.SetRootTheme(XamlRoot?, bool)` — set theme (`true` = dark)
- `SystemThemeHelper.SetApplicationTheme(XamlRoot?, ElementTheme)` — set theme using ElementTheme enum
- **No `ThemeChanged` event exists** — there is no event to subscribe to for theme changes
- Namespace: `using Uno.Toolkit.UI;`

## Related Skills

- [[uno-themes-semantic-colors-brushes]] — Semantic color palette and brush system
