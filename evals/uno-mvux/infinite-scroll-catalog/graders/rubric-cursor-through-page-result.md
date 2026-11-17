---
type: llm
---
The reply adds scroll-driven loading to an Uno Platform MVUX page over a cursor-based API.
Facts the judge needs: MVUX has a cursor (keyset) paginated list feed on the generic class, `ListFeed<T>.PaginatedByCursorAsync(firstPage, getPage)` (older docs: `ListFeed<T>.AsyncPaginatedByCursor`). `getPage` is `(TCursor cursor, uint? desiredPageSize, CancellationToken ct)` and returns a `PageResult<TCursor, T>` holding the page's items and the next cursor; MVUX keeps the cursor between requests and stops when the next cursor is the default. A ListView bound to the feed requests pages as the user scrolls, with no extra code.
PASS if the next cursor travels through `PageResult` (the API's `nextCursor`, null/default ending the list).
FAIL if the cursor lives in a mutable field or state that the delegate reads and writes, for example with the index-based `PaginatedAsync`.
