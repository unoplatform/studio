---
name: uno-toolkit-resource-extensions
description: "Use ResourceExtensions to apply ResourceDictionary directly to control styles for lightweight styling. Enables visual variants without page-level resources."
when_to_use: "Use when applying a `ResourceDictionary` directly to a single control so it picks up a visual variant without declaring resources at page level — typically per-control color or brush overrides, or shared-base-style variants."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Resource Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ResourceExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ResourceExtensions ResourceDictionary style lightweight inline")
```

Primary documentation pages:
- **ResourceExtensions (Chefs)**: `external/uno.chefs/doc/toolkit/ResourceExtensions.md`

Fetch the Chefs page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/toolkit/ResourceExtensions.md")
```

### Step 2: For Reference Documentation

```
uno_platform_docs_search("ResourceExtensions Resources attached property dictionary override")
```

## Key Principles (Stable)

- `utu:ResourceExtensions.Resources` — attaches a ResourceDictionary to a control
- Enables per-control resource key overrides without nesting in page resources
- Great for creating multiple visual variants of the same control
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-lightweight-styling]] — General lightweight styling approach
- [[uno-themes-material]] — Material lightweight styling
