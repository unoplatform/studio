---
type: llm
---
The reply gives a sequence of Uno App MCP (`uno_app_*`) calls to fill and submit a form in a running Uno Platform app.
Facts the judge needs: the check after the action is `uno_app_find_elements(text: "Welcome!" or automationId, waitForMs / until)`.
PASS if success is confirmed with `uno_app_find_elements` waiting on the "Welcome!" text (or its automation id).
FAIL if it confirms success by a screenshot alone.
