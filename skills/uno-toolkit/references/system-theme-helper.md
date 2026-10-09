# Uno Toolkit SystemThemeHelper

## Workflow

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
- `SystemThemeHelper.IsRootInDarkMode(XamlRoot)` — quick check if dark mode is active; non-nullable, with `FrameworkElement` and `Window` overloads
- `SystemThemeHelper.SetRootTheme(XamlRoot?, bool)` — set theme (`true` = dark)
- `SystemThemeHelper.SetApplicationTheme(XamlRoot?, ElementTheme)` — set theme using ElementTheme enum
- Obsolete, do not use: `GetApplicationTheme()`, `IsAppInDarkMode()`, `SetApplicationTheme(bool)`, `ToggleApplicationTheme()`
- **No `ThemeChanged` event exists** — there is no event to subscribe to for theme changes
- Namespace: `using Uno.Toolkit.UI;`

## Related Skills

- the `uno-themes` skill (`references/semantic-colors-brushes.md`) — Semantic color palette and brush system
