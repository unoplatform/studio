---
name: uno-toolkit-material-theme
description: "Configure Uno Material Toolkit with MaterialToolkitTheme for Material Design 3 styled Toolkit controls. Includes color and font customization."
when_to_use: "Use when wiring a Material Design 3 styled Uno Toolkit shell. `MaterialToolkitTheme` in App.xaml replaces the separate `MaterialTheme` + `ToolkitResources` pair and accepts color and font customisation in its constructor."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Material Theme — Agent Skill

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

- [[uno-toolkit-getting-started]] — Base Toolkit setup
- [[uno-themes-material]] — Material theme (non-Toolkit): installation, color palette customization, typography, control styles
