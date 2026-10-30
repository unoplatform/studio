---
type: llm
---
The reply explains how an agent using the Uno App MCP (the `uno_app_*` tools) should verify that a FeedView-backed list in a running Uno Platform app shows 12 items.
Uno App MCP facts the judge needs: the visual tree snapshot shows bindings, not current values, and a snapshot taken during loading sees the progress state; `uno_app_find_elements(automationId | name | text | type, waitForMs, until)` re-checks the live tree until a match or a state (`until: "absent"` for a loading indicator to go away, `until: "items>=12"` / `"items==12"` on the list) and reports each match's `{state}` such as `{items=N}`; `uno_app_get_element_datacontext` reads the bound model data. A FeedView goes through a progress template, then a value or error template.
PASS if all hold:
- It waits for the transient loading state to end in the app (e.g. `uno_app_find_elements` on the ProgressRing with `until: "absent"`, or on the list with `until: "items==12"`/`waitForMs`) before asserting, rather than asserting on the first snapshot.
- It asserts the exact count (12) from the list's `{items=N}` state or the DataContext, and checks that the error template is not what rendered.
- It locates elements by automation id or name (and suggests adding `AutomationProperties.AutomationId` if missing), not by screenshot.
FAIL if it relies on a fixed sleep/delay, on a screenshot to count items, or on repeated full snapshots as the main wait mechanism.
