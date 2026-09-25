# Uno Toolkit Material Theme

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

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
- Supports `ColorOverrideSource` and `FontOverrideSource` for customization
- Requires `Toolkit` in `<UnoFeatures>`

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`) — Material theme (non-Toolkit): installation, color palette customization, typography, control styles
