---
type: llm
---
The reply describes how an agent with the Uno App MCP checks a running Uno Platform desktop app at a narrow and a wide window size.
Facts the judge needs: `uno_app_get_screenshot(path: ...)` saves an image file inside the solution folder.
PASS if it saves one screenshot per layout to a file with `path`.
FAIL if it takes no screenshot, or only one for both layouts.
