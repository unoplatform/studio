---
type: llm
---
The reply gives a sequence of Uno App MCP (`uno_app_*`) calls to fill and submit a form in a running Uno Platform app.
Facts the judge needs: element tools accept `automationId` or `name` selectors directly.
PASS if elements are addressed by `automationId`/`name` selectors (or snapshot handles).
FAIL if it relies on pixel coordinates as the primary way to reach elements.
