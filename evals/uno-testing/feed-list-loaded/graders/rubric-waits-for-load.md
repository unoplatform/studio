---
type: llm
---
The reply explains how an agent using the Uno App MCP (the `uno_app_*` tools) should verify that a FeedView-backed list in a running Uno Platform app shows 12 items.
Facts the judge needs: the visual tree snapshot shows bindings, not current values, and a snapshot taken during loading sees the progress state; `uno_app_find_elements(automationId | name | text | type, waitForMs, until)` re-checks the live tree until a match or a state (`until: "absent"` for a loading indicator to go away, `until: "items>=12"` / `"items==12"` on the list) and reports each match's `{state}` such as `{items=N}`; `uno_app_get_element_datacontext` reads the bound model data. A FeedView goes through a progress template, then a value or error template.
PASS if it waits in the app for the transient loading state to end (for example `uno_app_find_elements` on the ProgressRing with `until: "absent"`, or on the list with `until: "items==12"`/`waitForMs`) before asserting.
FAIL if it asserts on the first snapshot, relies on a fixed sleep or delay, or uses repeated full snapshots as the main wait mechanism.
