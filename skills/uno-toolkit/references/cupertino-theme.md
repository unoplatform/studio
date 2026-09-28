# Uno Toolkit Cupertino Theme

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Cupertino Toolkit Documentation

```
uno_platform_docs_search("Uno Cupertino Toolkit CupertinoToolkitResources getting started")
```

Primary documentation page:
- **Cupertino Toolkit Getting Started**: `external/uno.toolkit.ui/doc/cupertino-getting-started.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/cupertino-getting-started.md")
```

## Key Principles (Stable)

- `CupertinoToolkitResources` (`xmlns="using:Uno.Toolkit.UI.Cupertino"`) is the resource dictionary for Cupertino-styled Toolkit controls; merge it alongside the Uno.Cupertino theme. There is no `CupertinoToolkitTheme` type.
- Place in App.xaml `<Application.Resources>`
- Requires `Toolkit` in `<UnoFeatures>`
- Pick one design language per app: `MaterialToolkitTheme` for Material, `CupertinoToolkitResources` for Cupertino

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- `references/material-theme.md` — Material alternative
