---
name: uno-toolkit-system-theme-helper
description: "Use SystemThemeHelper to detect system theme (light/dark), check if dark mode is active, and set application theme. Provides static methods for theme retrieval and management."
when_to_use: "Use when reading or programmatically switching dark / light mode from C# (not the XAML `ThemeResource` lookup path) — detecting the current system theme, setting the application theme at runtime, or toggling between modes."
metadata:
  author: uno-platform
  version: "2.5"
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

- **Preferred (hosted-safe): pass the app's own root element or Window.** These overloads behave identically in standalone apps and stay correct when the app's content is hosted under a XamlRoot it doesn't own - e.g. running under Hot Design, where `XamlRoot.Content` is the HOST's root visual and theming it re-themes the host instead of the app:
  - `SystemThemeHelper.GetRootTheme(FrameworkElement?)` / `GetRootTheme(Window?)`
  - `SystemThemeHelper.IsRootInDarkMode(FrameworkElement)` / `IsRootInDarkMode(Window)`
  - `SystemThemeHelper.SetRootTheme(FrameworkElement?, bool)` / `SetRootTheme(Window?, bool)` (`true` = dark)
  - `SystemThemeHelper.SetApplicationTheme(FrameworkElement?, ElementTheme)` / `SetApplicationTheme(Window?, ElementTheme)`
- Pass the app's ROOT element (typically `window.Content as FrameworkElement`, captured once at startup right after setting `window.Content`), not an arbitrary page.
- On Uno.Toolkit versions that don't have these overloads yet, set `appRoot.RequestedTheme = ElementTheme.Dark` directly on the captured root - same effect, hosted-safe.
- The `XamlRoot`-based overloads (`GetRootTheme(XamlRoot?)`, `IsRootInDarkMode(XamlRoot)`, `SetRootTheme(XamlRoot?, bool)`, `SetApplicationTheme(XamlRoot?, ElementTheme)`) target `XamlRoot.Content`; only use them in code that will never run hosted.
- `SystemThemeHelper.GetCurrentOsTheme()` - returns `ApplicationTheme.Light` or `ApplicationTheme.Dark`
- Null root: `Set*` overloads are silent no-ops (a warning is logged); `Get*` overloads fall back to the OS theme
- **No `ThemeChanged` event exists** - observe `FrameworkElement.ActualThemeChanged` on the app root instead
- Namespace: `using Uno.Toolkit.UI;`

## Related Skills

- [[uno-themes-semantic-colors-brushes]] — Semantic color palette and brush system
