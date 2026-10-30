---
type: llm
---
The reply explains how an agent should confirm a fix in a running Uno Platform app driven by the Uno App MCP, after editing the page model's C# and the page's XAML.
Facts the judge needs: after the app is started, edits to existing `.xaml` and `.cs` files reach it through Hot Reload; the agent saves and calls `uno_app_hot_reload_status(file: "<edited file>", waitForMs)` for each file: `applied` means the app runs the change (a C# change recreates the current page, so navigate back to the state under test); `NOT applied` comes with compiler diagnostics to fix; only a `RudeEdit`, an `InternalError`, or no operation covering the file after a second save calls for `uno_app_restart` (or close and start). A running app locks its bin folder, so `dotnet build` while it runs fails with MSB3026/MSB3027.
PASS if it restarts only when the status reports a rude edit, an internal error or an uncovered edit (or equivalent), and otherwise fixes the reported diagnostics and saves again.
FAIL if it restarts on any failed status, or never says when a restart is needed.
