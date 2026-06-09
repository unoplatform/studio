---
name: uno-toolkit-getting-started
description: "Install and configure Uno Toolkit library in Uno Platform applications."
when_to_use: "Use when setting up Uno Toolkit in a new or existing project, adding `<UnoFeatures>Toolkit</UnoFeatures>` to a project, configuring ToolkitResources or MaterialToolkitTheme in App.xaml, or understanding Toolkit NuGet package requirements."
metadata:
  author: uno-platform
  version: "2.3"
  category: toolkit
---

# Uno Toolkit Getting Started — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Getting Started Documentation

```
uno_platform_docs_search("Uno Toolkit getting started install UnoFeatures ToolkitResources")
```

Primary documentation pages:
- **Getting Started (base Toolkit)**: `external/uno.toolkit.ui/doc/getting-started.md`
- **Material Toolkit Getting Started**: `external/uno.toolkit.ui/doc/material-getting-started.md`

Fetch the getting started page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/getting-started.md")
```

### Step 2: For Material Design Toolkit

If user needs Material Design styled toolkit controls:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/material-getting-started.md")
```

### Step 3: For Available Controls and Helpers

To see the full list of Toolkit controls and helpers:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls-styles.md")
```

## Key Principles (Stable)

- Add `Toolkit` to `<UnoFeatures>` in the project file
- For Material styling, use `MaterialToolkitTheme` in App.xaml (replaces separate MaterialTheme + ToolkitResources)
- For Cupertino styling, use `CupertinoToolkitTheme`
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`
- CLI: `dotnet new unoapp -o MyApp -toolkit` to create a new project with Toolkit

## Related Skills

- [[uno-toolkit-material-theme]] — MaterialToolkitTheme configuration
- [[uno-toolkit-cupertino-theme]] — CupertinoToolkitTheme configuration
- [[uno-themes-material]] — Material theme installation
