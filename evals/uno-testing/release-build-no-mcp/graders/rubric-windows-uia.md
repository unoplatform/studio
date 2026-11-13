---
type: llm
---
The reply explains how to smoke-test a Release build of an Uno Platform (Skia) app on Windows and Android without the Uno App MCP.
Facts the judge needs: Uno exposes its automation peers to the OS accessibility tree. On Windows, UI Automation (for example `System.Windows.Automation` from PowerShell or .NET) sees `AutomationProperties.AutomationId` as `AutomationId` (falling back to `x:Name`) and `AutomationProperties.Name` as `Name`; act with Invoke/Value/Toggle patterns and read `Name`/`IsEnabled`.
PASS if on Windows it drives the app through UI Automation, finding controls by AutomationId (or Name), acting through patterns, and asserting on what the tree reports.
FAIL if on Windows it relies on screenshots or pixel matching as the main way to find controls or assert.
