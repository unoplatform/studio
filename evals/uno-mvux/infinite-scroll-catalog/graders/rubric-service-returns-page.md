---
type: llm
---
The reply adds scroll-driven loading to an Uno Platform MVUX page over a cursor-based API.
PASS if the service method takes the cursor, a limit and a `CancellationToken` and returns one page with its next cursor.
FAIL if the service method loads every product, or drops the cursor or the next cursor.
