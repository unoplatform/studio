# Uno Toolkit StatusBar Extensions

## Critical Rules

- **`StatusBar.Foreground` is a `StatusBarForegroundTheme` enum value, NOT a Brush.** Use attribute syntax with a bare enum name: `utu:StatusBar.Foreground="Light"` (or `Dark`, `Auto`, `AutoInverse`). Element syntax or a Brush value (e.g. `<utu:StatusBar.Foreground><SolidColorBrush Color="White"/></utu:StatusBar.Foreground>`) is **invalid**.
- `StatusBar.Background` *is* a color/brush — accepts an attribute color name or a `{StaticResource}` brush.
- The class is **`StatusBar`**, not `StatusBarExtensions`, despite the file/skill name.
- **On iOS, set `UIViewControllerBasedStatusBarAppearance` to `false` in `Platforms/iOS/Info.plist`** (`<key>UIViewControllerBasedStatusBarAppearance</key><false/>`). Without it the properties do nothing on iOS.

## Key Principles (Stable)

- `utu:StatusBar.Foreground` — StatusBarForegroundTheme enum value
- `utu:StatusBar.Background` — color or brush
- StatusBarForegroundTheme values: `None`, `Light` (white icons), `Dark` (black icons), `Auto` (matches theme, updates on change), `AutoInverse` (opposite of theme, updates on change)
- Applied as attached properties on the Page element
- Works on iOS and Android only
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the StatusBar Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit StatusBar extensions foreground background color iOS Android")
```

Primary documentation page:
- **StatusBar Extensions**: `external/uno.toolkit.ui/doc/helpers/StatusBar-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/StatusBar-extensions.md", anchor="…")
```

## Related Skills

- `references/safearea.md` — Safe area insets for status bar
