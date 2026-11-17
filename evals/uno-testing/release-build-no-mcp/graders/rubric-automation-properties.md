---
type: llm
---
The reply explains how to smoke-test a Release build of an Uno Platform (Skia) app on Windows and Android without the Uno App MCP.
Facts the judge needs: Android exposes `AutomationProperties.Name`, not the AutomationId, on Uno 6.x.
PASS if for the XAML it adds `AutomationProperties.AutomationId` and a readable, unique `AutomationProperties.Name` to the controls the test touches, noting that Android relies on the Name.
FAIL if it adds only `AutomationId`, or no automation properties.
