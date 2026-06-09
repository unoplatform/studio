---
name: uno-toolkit-itemsrepeater-extensions
description: "Attached properties (`utu:ItemsRepeaterExtensions.SelectionMode`, `.SelectedItem`, `.SelectedItems`, `.SelectedIndex`, `.SelectedIndexes`) that add selection and incremental loading to `ItemsRepeater`."
when_to_use: "Use when `ItemsRepeater` needs selection or fetch-on-scroll — `ItemsRepeater` itself has no built-in `SelectionMode` / `SelectedItem`, so the Toolkit's attached properties are the canonical way to add them. Pairs with `ISupportIncrementalLoading` on the data source for automatic page loading."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit ItemsRepeater Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ItemsRepeater Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit ItemsRepeater extensions selection SelectedItem incremental loading")
```

Primary documentation:

```
uno_platform_docs_search("ItemsRepeaterExtensions SelectedItem SelectedItems SelectionMode incremental")
```

### Step 2: For Controls Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls-styles.md")
```

Look for ItemsRepeaterExtensions in the helpers section.

## Key Principles (Stable)

- `utu:ItemsRepeaterExtensions.SelectedItem` — single selected item (bindable)
- `utu:ItemsRepeaterExtensions.SelectedIndex` — single selection index (bindable)
- `utu:ItemsRepeaterExtensions.SelectedItems` — multiple selected items (bindable)
- `utu:ItemsRepeaterExtensions.SelectedIndexes` — multiple selection indexes (bindable)
- `utu:ItemsRepeaterExtensions.SelectionMode` — Single, Multiple, None
- Supports `ISupportIncrementalLoading` for automatic data fetching on scroll
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-selector-extensions]] — Selection on Selector-based controls
- [[uno-toolkit-command-extensions]] — Command on ItemsRepeater tap
