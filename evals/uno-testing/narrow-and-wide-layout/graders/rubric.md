---
type: llm
---
The reply describes how an agent with the Uno App MCP checks a running Uno Platform desktop app at a narrow and a wide window size.
Uno App MCP facts the judge needs: `uno_app_start(..., windowSize: "1280x800", connectionTimeoutSeconds)` opens the app at a client-area size and `uno_app_resize(width, height)` changes it while it runs (logical pixels, desktop only), so two layouts are two resize calls on one running instance, not two starts; resizing from `OnLaunched` or with a Win32/UI Automation script is the wrong tool when these exist. State is asserted with `uno_app_find_elements` (e.g. the details pane by automation id with `includeHidden: false` returning 0 matches, or `!hidden`; the "Show details" button present), and `uno_app_get_screenshot(path: ...)` saves an image file inside the solution folder for evidence.
PASS if all hold:
- It sizes the window through the App MCP (`uno_app_resize`, optionally `windowSize` at start) on one running app, below and above 720 px.
- After each resize it asserts the layout state from the element tree (pane hidden/visible, button present/absent) via `uno_app_find_elements`, waiting for the change rather than asserting immediately.
- It saves one screenshot per layout to a file with `path`.
FAIL if it restarts or rebuilds the app to change size, changes the window size in app code, or uses screenshots alone to decide which layout is showing.
