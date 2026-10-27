---
type: llm
---
The reply explains how an agent should confirm a fix in a running Uno Platform app driven by the Uno App MCP.
Uno App MCP facts the judge needs: after the app is started, edits to existing `.xaml` and `.cs` files reach it through Hot Reload (new members and classes usually apply too); the agent saves and calls `uno_app_hot_reload_status(file: "<edited file>", waitForMs)` for each file: `applied` means the app runs the change (a C# change recreates the current page, so navigate back to the state under test); `NOT applied` comes with compiler diagnostics to fix; only a `RudeEdit`, an `InternalError`, or no operation covering the file after a second save calls for `uno_app_restart` (or close and start). A running app locks its bin folder, so `dotnet build` while it runs fails with MSB3026/MSB3027. The re-check is `uno_app_find_elements(automationId: "<save button>", until: "enabled")`.
PASS if all hold:
- It does not rebuild or relaunch by default: it saves and checks `uno_app_hot_reload_status` for each edited file.
- It restarts only when the status reports a rude edit / internal error / uncovered edit (or equivalent), and otherwise fixes diagnostics and saves again.
- It re-runs the failed assertion in the app (Save button enabled via `uno_app_find_elements` with `until`/`waitForMs`, after re-entering valid input if the page was recreated) and checks the TextBlock text.
FAIL if it rebuilds or restarts the app as the normal path, runs `dotnet build` while the app is running, or declares the fix confirmed from the code alone.
