---
type: llm
---
The reply describes how an agent with the Uno App MCP checks a running Uno Platform desktop app at a narrow and a wide window size.
Facts the judge needs: `uno_app_start(..., windowSize: "1280x800", connectionTimeoutSeconds)` opens the app at a client-area size and `uno_app_resize(width, height)` changes it while it runs (logical pixels, desktop only), so two layouts are two resize calls on one running instance, not two starts; resizing from `OnLaunched` or with a Win32/UI Automation script is the wrong tool when these exist.
PASS if it sizes the window through the App MCP (`uno_app_resize`, optionally `windowSize` at start) on one running app, below and above 720 px.
FAIL if it restarts or rebuilds the app to change size, or changes the window size in app code.
