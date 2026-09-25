---
name: uno-toolkit-divider
description: "Divider for thin-line visual separators between content, list, form, or settings sections. Use when separating sections with a thin rule, with or without optional subheader text (a subheader label). Supports horizontal and vertical orientations and lightweight styling via resource keys."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Divider — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Divider Documentation

```
uno_platform_docs_search("Uno Toolkit Divider separator thin line subheader")
```

Primary documentation page:
- **Divider**: `external/uno.toolkit.ui/doc/controls/Divider.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/Divider.md")
```

## Key Principles (Stable)

- Simple thin line separator
- Optional `SubHeader` text property
- Supports lightweight styling via resource keys
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-lightweight-styling]] — Customizing divider appearance
