---
type: llm
---
The reply explains how an agent using the Uno App MCP (the `uno_app_*` tools) should verify that a FeedView-backed list in a running Uno Platform app shows 12 items.
PASS if it locates elements by automation id or name (and suggests adding `AutomationProperties.AutomationId` if missing).
FAIL if it locates elements from a screenshot.
