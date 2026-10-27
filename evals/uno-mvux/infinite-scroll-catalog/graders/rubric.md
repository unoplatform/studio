---
type: llm
---
The reply adds scroll-driven loading to an Uno Platform MVUX page over a cursor-based API.
Facts the judge needs: MVUX has a cursor (keyset) paginated list feed on the generic class, `ListFeed<T>.PaginatedByCursorAsync(firstPage, getPage)` (older docs: `ListFeed<T>.AsyncPaginatedByCursor`). `getPage` is `(TCursor cursor, uint? desiredPageSize, CancellationToken ct)` and returns a `PageResult<TCursor, T>` holding the page's items and the next cursor; MVUX keeps the cursor between requests and stops when the next cursor is the default. A ListView bound to the feed requests pages as the user scrolls, with no extra code. `desiredPageSize` is null on the first request, so it needs a fallback.
PASS if all hold:
- The Model is a `partial record` exposing an `IListFeed<T>` built with the cursor-paginated factory, and the ListView binds to it (directly or through a FeedView).
- The next cursor travels through `PageResult` (the API's `nextCursor`, null/default ending the list), not through a mutable field or state the delegate reads and writes.
- The limit comes from `desiredPageSize ?? <default>` (or a fixed page size), and the service method takes the cursor, a limit and a `CancellationToken` and returns one page with its next cursor.
FAIL if it uses the index-based `PaginatedAsync` with a cursor stored in a field, writes its own `ISupportIncrementalLoading` collection, uses an `ObservableCollection` with a scroll handler, or a "load more" button instead of the paginated feed.
