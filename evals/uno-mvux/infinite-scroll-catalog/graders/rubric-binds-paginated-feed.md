---
type: llm
---
The reply adds scroll-driven loading to an Uno Platform MVUX page over a cursor-based API.
Facts the judge needs: MVUX has a cursor (keyset) paginated list feed on the generic class, `ListFeed<T>.PaginatedByCursorAsync(firstPage, getPage)` (older docs: `ListFeed<T>.AsyncPaginatedByCursor`). `getPage` is `(TCursor cursor, uint? desiredPageSize, CancellationToken ct)` and returns a `PageResult<TCursor, T>` holding the page's items and the next cursor; MVUX keeps the cursor between requests and stops when the next cursor is the default. A ListView bound to the feed requests pages as the user scrolls, with no extra code.
PASS if the Model is a `partial record` exposing an `IListFeed<T>` built with the cursor-paginated factory, and the ListView binds to it (directly or through a FeedView).
FAIL if it writes its own `ISupportIncrementalLoading` collection, uses an `ObservableCollection` with a scroll handler, or a "load more" button instead of the paginated feed.
