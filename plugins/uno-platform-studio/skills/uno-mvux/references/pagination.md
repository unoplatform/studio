# MVUX Pagination

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
- **Index-based**: `ListFeed.PaginatedAsync(async (request, ct) => ...)`; the delegate is `AsyncFunc<PageRequest, IImmutableList<T>>`, so it takes the request and a `CancellationToken`, and a one-parameter lambda does not compile. `PageRequest` has `uint Index`, `uint CurrentCount`, and `uint? DesiredSize` (null on the first page, so fall back to a default). Slice with `Skip((int)request.CurrentCount).Take((int)(request.DesiredSize ?? DefaultPageSize))`, not `Index * DesiredSize`.
- **Cursor/keyset-based**: `ListFeed<T>.PaginatedByCursorAsync<TCursor>(firstPage, getPage)` on the generic `ListFeed<T>` class only, where `getPage` is a `GetPage<TCursor, T>`

### Step 4: For ItemsRepeater Integration

If the user is using `ItemsRepeater` for incremental loading:

```
uno_platform_docs_search("ItemsRepeater MVUX pagination incremental loading")
```

Key page:
- **ItemsRepeater Extensions**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/itemsrepeater-extensions.howto.md`

## Key Principles (Stable)

- Use `ListFeed.PaginatedAsync(...)` to create a paginated list feed
- The callback receives a `PageRequest`; skip `CurrentCount` items and take `DesiredSize` (nullable on the first page)
- `ListView` supports incremental loading automatically when bound to a paginated feed
- For `ItemsRepeater`, use the Toolkit's `ItemsRepeaterExtensions` for incremental loading support
- Pagination works with both index-based and cursor-based APIs

## Related Skills

- `references/listfeed.md` — Non-paginated list feeds
- `references/selection.md` — Selection with paginated lists
- the `uno-toolkit` skill (`references/itemsrepeater-extensions.md`) — ItemsRepeater incremental loading
