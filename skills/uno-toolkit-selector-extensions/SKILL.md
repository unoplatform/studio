---
name: uno-toolkit-selector-extensions
description: "Use SelectorExtensions to sync a Selector control (FlipView, ListView) with a PipsPager. The PipsPager attached property auto-syncs NumberOfPages and SelectedIndex."
when_to_use: "Use when synchronising a `PipsPager` indicator with a `FlipView` or `ListView` selection — carousel and slideshow UIs. The attached property auto-syncs `NumberOfPages` and `SelectedIndex` between the two controls."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Selector Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the SelectorExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit SelectorExtensions PipsPager Selector sync FlipView")
```

Primary documentation page:
- **Selector Extensions**: `external/uno.toolkit.ui/doc/helpers/Selector-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/Selector-extensions.md")
```

## Key Principles (Stable)

- Only one attached property: `utu:SelectorExtensions.PipsPager`
- The property must be set on the **Selector control** (FlipView, ListView), **NOT** on the PipsPager
- Automatically syncs `NumberOfPages` and `SelectedIndex` bidirectionally
- Swiping the Selector updates the pip; clicking a pip navigates the Selector
- Do not manually set `NumberOfPages` on the PipsPager — it is auto-managed
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-flipview-extensions]] — Previous/Next buttons for FlipView
- [[uno-toolkit-itemsrepeater-extensions]] — Selection for ItemsRepeater
