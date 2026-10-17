# Uno Toolkit Material Theme

## Key Principles (Stable)

- `MaterialToolkitTheme` is the unified approach — replaces separate `MaterialTheme` + `ToolkitResources`
- Place in App.xaml `<Application.Resources>` section
- Customize colors through `MaterialToolkitTheme.Colors` (`<ut:ThemeColors xmlns:ut="using:Uno.Themes" OverrideSource="..." />`; `ColorOverrideSource` is obsolete in Uno.Themes 7) and fonts through `FontOverrideSource`
- Requires `Toolkit;Material` in `<UnoFeatures>`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Material Toolkit Getting Started

```
uno_platform_docs_search("Uno Material Toolkit MaterialToolkitTheme configuration App.xaml")
```

Primary documentation page:
- **Material Toolkit Getting Started**: `external/uno.toolkit.ui/doc/material-getting-started.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/material-getting-started.md", anchor="…")
```

### For Color/Font Customization

Customization is done via `MaterialToolkitTheme` properties:

```
uno_platform_docs_search("MaterialToolkitTheme color font override customize palette")
```

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`, `references/material-customization.md`) — Material theme (non-Toolkit): installation and control styles; color palette and font customization
