---
name: uno-toolkit-lightweight-styling
description: "Apply lightweight styling to Uno Toolkit controls by overriding resource keys. Change colors, fonts, spacing without custom styles."
when_to_use: "Use when customising Toolkit control appearance by overriding resource keys (colors, fonts, spacing) rather than rewriting templates. Overrides may live at app, page, or per-control scope; the main task is finding the right resource key in the naming pattern."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Lightweight Styling — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Lightweight Styling Documentation

```
uno_platform_docs_search("Uno Toolkit lightweight styling resource keys override controls")
```

Primary documentation page:
- **Toolkit Lightweight Styling**: `external/uno.toolkit.ui/doc/lightweight-styling.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/lightweight-styling.md")
```

### Step 2: For Specific Control Resource Keys

Each control's documentation page lists its resource keys. Search for the specific control:

```
uno_platform_docs_search("Uno Toolkit [ControlName] lightweight styling resource keys")
```

## Key Principles (Stable)

- Override resource keys at App, Page, or Control level
- Resource keys follow naming patterns: `{ControlName}{State}{Property}`
- Control-level overrides use `<Control.Resources>` or `ResourceExtensions`
- No need to redefine entire styles/templates
- Each control's doc page lists available resource keys

## Related Skills

- [[uno-toolkit-resource-extensions]] — Per-control resource dictionary
- [[uno-themes-material]] — Material design lightweight styling
