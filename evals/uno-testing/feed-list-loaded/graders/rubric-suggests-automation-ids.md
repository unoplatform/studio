---
type: llm
---
The reply explains how an agent using the Uno App MCP (the `uno_app_*` tools) should verify that a FeedView-backed list in a running Uno Platform app shows 12 items. The user's existing app is not shown, so the reply cannot know which controls carry an automation id.
PASS if it tells the user to add `AutomationProperties.AutomationId` (or `x:Name`/`AutomationProperties.Name`) to the controls the check uses when they don't have one.
FAIL if it assumes the ids it uses already exist and never says to add them.
