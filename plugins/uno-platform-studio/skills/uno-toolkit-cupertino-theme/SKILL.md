---
name: uno-toolkit-cupertino-theme
description: "Configure Uno Cupertino Toolkit with CupertinoToolkitTheme for iOS-style Toolkit controls."
when_to_use: "Use when targeting iOS-styled UI and replacing `MaterialToolkitTheme` with `CupertinoToolkitTheme` in App.xaml, implementing Apple Human Interface Guidelines styling, or applying the Cupertino design system to Toolkit controls."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Cupertino Theme — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Cupertino Toolkit Documentation

```
uno_platform_docs_search("Uno Cupertino Toolkit CupertinoToolkitTheme getting started")
```

Primary documentation page:
- **Cupertino Toolkit Getting Started**: `external/uno.toolkit.ui/doc/cupertino-getting-started.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/cupertino-getting-started.md")
```

## Key Principles (Stable)

- `CupertinoToolkitTheme` is the unified approach for Cupertino-styled Toolkit controls
- Place in App.xaml `<Application.Resources>`
- Requires `Toolkit` in `<UnoFeatures>`
- Cupertino and Material themes are mutually exclusive

## Related Skills

- [[uno-toolkit-getting-started]] — Base Toolkit setup
- [[uno-toolkit-material-theme]] — Material alternative
