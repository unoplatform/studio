---
type: llm
---
The reply shows loading, error and retry UI for an MVUX feed in an Uno Platform page.
PASS if loading and error states are left to the FeedView.
FAIL if the Model hand-manages them with `IsLoading`/`HasError` properties or a try/catch.
