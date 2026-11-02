---
type: llm
---
The reply explains how an agent using the Uno App MCP (the `uno_app_*` tools) should verify that a FeedView-backed list in a running Uno Platform app shows 12 items.
PASS if it locates the elements it checks by selector (automation id, name or type) through the `uno_app_*` tools.
FAIL if it locates elements from a screenshot.
