---
name: uno-mvux-pagination
description: "Implement paginated and infinite scrolling lists in MVUX."
when_to_use: "Use when loading large datasets incrementally (page by page), implementing infinite scroll in `ListView`, `GridView`, or `ItemsRepeater`, working with APIs that use offset/limit or cursor-based pagination, or using `ListFeed.PaginatedAsync(...)`."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# MVUX Pagination — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Pagination Documentation

Search for and fetch the pagination documentation:

```
uno_platform_docs_search("MVUX pagination infinite scrolling PaginatedAsync")
```

Primary documentation pages:
- **Pagination Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Pagination.md`
- **Pagination in Chefs App**: `external/uno.chefs/doc/mvux/Pagination.md`
- **Usage in Apps (Pagination section)**: `external/uno.extensions/doc/Reference/Reactive/in-apps.md`

Fetch the reference page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Pagination.md")
```

### Step 2: For Practical Examples

Fetch the Chefs app example for a real-world implementation:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/mvux/Pagination.md")
```

### Step 3: Understand Pagination Types

From the fetched docs:
- **Index-based**: Uses page size and start index (`PageRequest.DesiredSize`, `PageRequest.CurrentCount`)
- **Cursor/keyset-based**: Uses a cursor token for the next page

### Step 4: For ItemsRepeater Integration

If the user is using `ItemsRepeater` for incremental loading:

```
uno_platform_docs_search("ItemsRepeater MVUX pagination incremental loading")
```

Key page:
- **ItemsRepeater Extensions**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/itemsrepeater-extensions.howto.md`

## Key Principles (Stable)

- Use `ListFeed.PaginatedAsync(...)` to create a paginated list feed
- The callback receives a `PageRequest` with `DesiredSize` and `CurrentCount`
- `ListView` supports incremental loading automatically when bound to a paginated feed
- For `ItemsRepeater`, use the Toolkit's `ItemsRepeaterExtensions` for incremental loading support
- Pagination works with both index-based and cursor-based APIs

## Related Skills

- [[uno-mvux-listfeed]] — Non-paginated list feeds
- [[uno-mvux-selection]] — Selection with paginated lists
- [[uno-toolkit-itemsrepeater-extensions]] — ItemsRepeater incremental loading
