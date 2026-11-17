---
type: llm
---
The reply describes how an agent with the Uno App MCP checks a running Uno Platform desktop app at a narrow and a wide window size.
Facts the judge needs: state is asserted with `uno_app_find_elements` (for example the details pane by automation id with `includeHidden: false` returning 0 matches, or `!hidden`; the "Show details" button present).
PASS if after each resize it asserts the layout state from the element tree (pane hidden or visible, button present or absent) via `uno_app_find_elements`, waiting for the change rather than asserting immediately.
FAIL if it uses screenshots alone to decide which layout is showing.
