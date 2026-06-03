---
name: uno-toolkit-statusbar-extensions
description: "Attached properties (`StatusBar.Foreground`, `StatusBar.Background`) for customising the system status bar from XAML on mobile platforms."
when_to_use: "Use when customising the system status bar on iOS or Android — light vs dark icons, background tint, or visibility — per page from XAML."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit StatusBar Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the StatusBar Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit StatusBar extensions foreground background color iOS Android")
```

Primary documentation page:
- **StatusBar Extensions**: `external/uno.toolkit.ui/doc/helpers/StatusBar-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/StatusBar-extensions.md")
```

## Critical Rules

- **`StatusBar.Foreground` is a `StatusBarForegroundTheme` enum value, NOT a Brush.** Use attribute syntax with a bare enum name: `utu:StatusBar.Foreground="Light"` (or `Dark`, `Auto`, `AutoInverse`). Element syntax or a Brush value (e.g. `<utu:StatusBar.Foreground><SolidColorBrush Color="White"/></utu:StatusBar.Foreground>`) is **invalid**.
- `StatusBar.Background` *is* a color/brush — accepts an attribute color name or a `{StaticResource}` brush.
- The class is **`StatusBar`**, not `StatusBarExtensions`, despite the file/skill name.

## Key Principles (Stable)

- `utu:StatusBar.Foreground` — StatusBarForegroundTheme enum value
- `utu:StatusBar.Background` — color or brush
- StatusBarForegroundTheme values: `Light` (white icons), `Dark` (black icons), `Auto` (matches theme, updates on change), `AutoInverse` (opposite of theme, updates on change)
- Applied as attached properties on the Page element
- Works on iOS and Android only
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-safearea]] — Safe area insets for status bar
