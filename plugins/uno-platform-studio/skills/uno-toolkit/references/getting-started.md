# Uno Toolkit Getting Started

## Workflow

### Step 1: Fetch the Getting Started Documentation

```
uno_platform_docs_search("Uno Toolkit getting started install UnoFeatures ToolkitResources")
```

Primary documentation pages:
- **Getting Started (base Toolkit)**: `external/uno.toolkit.ui/doc/getting-started.md`
- **Material Toolkit Getting Started**: `external/uno.toolkit.ui/doc/material-getting-started.md`
- **Simple Toolkit Getting Started**: `external/uno.toolkit.ui/doc/simple-getting-started.md`

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

- Add `Toolkit` to `<UnoFeatures>` in the project file, plus the theme feature (`Material`, `SimpleTheme`, or `Cupertino`)
- For Material styling, use `MaterialToolkitTheme` in App.xaml (replaces separate MaterialTheme + ToolkitResources)
- For Simple styling (the recommended template preset), use `<utus:SimpleToolkitTheme xmlns:utus="using:Uno.Toolkit.UI.Simple" />` (replaces SimpleTheme + ToolkitResources)
- For Cupertino styling, merge the `Uno.Cupertino` dictionaries, then `ToolkitResources` and `CupertinoToolkitResources` (see `references/cupertino-theme.md`)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`
- CLI: `dotnet new unoapp -o MyApp -toolkit` to create a new project with Toolkit

## Related Skills

- `references/material-theme.md` — MaterialToolkitTheme configuration
- `references/cupertino-theme.md` — CupertinoToolkitResources configuration
- the `uno-themes` skill (`references/simple.md`) — SimpleToolkitTheme properties
- the `uno-themes` skill (`references/material.md`) — Material theme installation
