---
type: llm
---
The reply explains how an agent should confirm a fix in a running Uno Platform app driven by the Uno App MCP, after editing the page model's C# and the page's XAML.
PASS if it also checks the corrected TextBlock text in the running app.
FAIL if it never checks the XAML fix in the app.
