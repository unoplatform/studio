---
type: llm
---
The reply explains how to smoke-test a Release build of an Uno Platform (Skia) app on Windows and Android without the Uno App MCP.
Facts the judge needs: the Uno App MCP works with Debug builds only.
PASS if it treats the App MCP as unavailable for the Release build.
FAIL if it claims the App MCP works in Release.
