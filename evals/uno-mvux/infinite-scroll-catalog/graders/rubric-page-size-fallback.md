---
type: llm
---
The reply adds scroll-driven loading to an Uno Platform MVUX page over a cursor-based API.
Facts the judge needs: `desiredPageSize` is null on the first request, so it needs a fallback.
PASS if the limit comes from `desiredPageSize ?? <default>` or a fixed page size.
FAIL if it passes `desiredPageSize` to the API with no fallback for null.
