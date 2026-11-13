# MVUX Pagination

**Contents:** [Key Principles (Stable)](#key-principles-stable) · [Understand Pagination Types](#understand-pagination-types) · [Cursor pagination with a next link](#cursor-pagination-with-a-next-link) · [Search with pagination](#search-with-pagination) · [Docs, for what this page does not cover](#docs-for-what-this-page-does-not-cover) · [Related Skills](#related-skills)

**Any list backed by a server or an API is a paginated list feed.** Do not load every row into memory and filter it on the client: the list loads a page as the user scrolls, and a search sends the term to the server. A plain `ListFeed.Async` is for data that is local or known to stay small.

**Totals over one table do not make every list load whole.** When a dashboard needs counts or sums over every row of a table, ask the server for them where it can count or aggregate, or load that one table whole and record why. The lists the totals do not need (pickers and lookups, related records, tables shown only as a count) still page.

## Key Principles (Stable)

- Use `ListFeed.PaginatedAsync(...)` to create a paginated list feed
- The callback receives a `PageRequest`; skip `CurrentCount` items and take `DesiredSize` (nullable on the first page)
- `ListView` supports incremental loading automatically when bound to a paginated feed
- For `ItemsRepeater`, use the Toolkit's `ItemsRepeaterExtensions` for incremental loading support
- Pagination works with both index-based and cursor-based APIs

## Understand Pagination Types

- **Index-based**: `ListFeed.PaginatedAsync(async (request, ct) => ...)`; the delegate is `AsyncFunc<PageRequest, IImmutableList<T>>`, so it takes the request and a `CancellationToken`, and a one-parameter lambda does not compile. `PageRequest` has `uint Index`, `uint CurrentCount`, and `uint? DesiredSize` (null on the first page, so fall back to a default). Slice with `Skip((int)request.CurrentCount).Take((int)(request.DesiredSize ?? DefaultPageSize))`, not `Index * DesiredSize`.
- **Cursor/keyset-based**: `ListFeed<T>.PaginatedByCursorAsync<TCursor>(firstPage, getPage)` on the generic `ListFeed<T>` class only, where `getPage` is a `GetPage<TCursor, T>`: `(cursor, uint? desiredPageSize, ct)` returning `ValueTask<PageResult<TCursor, T>>` (namespace `Uno.Extensions.Reactive.Sources`). Use it for an API that returns a next link or a continuation token, and for any API that cannot skip (Dataverse rejects `$skip`).

## Cursor pagination with a next link

The cursor is the next link. Start from `string.Empty` (the service reads it as "first page") and return the items with the server's next link, `null` on the last page: a `null` `NextPage` ends the list.

```csharp
public IListFeed<Contact> Contacts => ListFeed<Contact>.PaginatedByCursorAsync(
    firstPage: string.Empty,
    getPage: async (nextLink, desiredSize, ct) =>
    {
        var (items, next) = await Service.GetPageAsync(nextLink, desiredSize ?? 50, ct);
        return new PageResult<string, Contact>(items, next);
    });
```

With a Kiota OData client the service requests the first page with `Prefer: odata.maxpagesize=N` and follows `OdataNextLink` with the request builder's `WithUrl(nextLink).GetAsync(...)`.

## Search with pagination

Project the search term into the paginated list with `SelectPaginatedByCursorAsync` (or `SelectPaginatedAsync` for index pages). A new term resets the list to its first page, and the term goes to the server's filter.

```csharp
public IState<string> Search => State.Value(this, () => string.Empty);

public IListFeed<Contact> Results => Search.SelectPaginatedByCursorAsync(
    firstPage: string.Empty,
    getPage: async (term, nextLink, desiredSize, ct) =>
    {
        var (items, next) = await Service.SearchAsync(term, nextLink, desiredSize ?? 50, ct);
        return new PageResult<string, Contact>(items, next);
    });
```

Sort is one more input to the same projection: hold the term and the sort (column, direction) in one record state and project that, so a new sort restarts from the first page too. The sort goes to the server (`$orderby` in OData), never to an `OrderBy` over the pages loaded so far.

**Never start `SelectPaginatedByCursorAsync` from a `null` cursor.** It then never calls `getPage` and the list stays empty. `ListFeed<T>.PaginatedByCursorAsync` does load a `null` first cursor; `string.Empty` works in both.

`SelectPaginatedAsync(async (term, request, ct) => ...)` is the index form, with the same `PageRequest` as above. Bind the `TextBox` to `Search` and the `ListView` to `Results`. Verified with Uno.Extensions.Reactive 8.0.0-dev.250: both snippets compile, and the `null` first cursor behavior was run.

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Pagination Documentation

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
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Pagination.md", anchor="…")
```

### For Practical Examples

Fetch the Chefs app example for a real-world implementation:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/mvux/Pagination.md", anchor="…")
```

### For ItemsRepeater Integration

If the user is using `ItemsRepeater` for incremental loading:

```
uno_platform_docs_search("ItemsRepeater MVUX pagination incremental loading")
```

Key page:
- **ItemsRepeater Extensions**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/itemsrepeater-extensions.howto.md`

## Related Skills

- `references/listfeed.md` — Non-paginated list feeds
- `references/selection.md` — Selection with paginated lists
- the `uno-toolkit` skill (`references/itemsrepeater-extensions.md`) — ItemsRepeater incremental loading
