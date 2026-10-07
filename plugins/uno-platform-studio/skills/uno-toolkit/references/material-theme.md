# Uno Toolkit Material Theme

## Workflow

### Step 1: Fetch the Material Toolkit Getting Started

```
uno_platform_docs_search("Uno Material Toolkit MaterialToolkitTheme configuration App.xaml")
```

Primary documentation page:
- **Material Toolkit Getting Started**: `external/uno.toolkit.ui/doc/material-getting-started.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/material-getting-started.md")
```

### Step 2: For Color/Font Customization

Customization is done via `MaterialToolkitTheme` properties:

```
uno_platform_docs_search("MaterialToolkitTheme color font override customize palette")
```

## Key Principles (Stable)

- `MaterialToolkitTheme` is the unified approach — replaces separate `MaterialTheme` + `ToolkitResources`
- Place in App.xaml `<Application.Resources>` section
- Customize colors through `MaterialToolkitTheme.Colors` (`<ut:ThemeColors xmlns:ut="using:Uno.Themes" OverrideSource="..." />`; `ColorOverrideSource` is obsolete in Uno.Themes 7) and fonts through `FontOverrideSource`
- Requires `Toolkit;Material` in `<UnoFeatures>`

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`, `references/material-customization.md`) — Material theme (non-Toolkit): installation and control styles; color palette and font customization
