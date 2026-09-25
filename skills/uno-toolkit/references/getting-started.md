# Uno Toolkit Getting Started

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

- `references/material-theme.md` — MaterialToolkitTheme configuration
- `references/cupertino-theme.md` — CupertinoToolkitTheme configuration
- the `uno-themes` skill (`references/material.md`) — Material theme installation
