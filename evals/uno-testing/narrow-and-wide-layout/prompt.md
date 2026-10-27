---
description: Checking a responsive page at two window sizes resizes the running app through the App MCP, asserts the layout change from the tree, and saves screenshots, without restarting or resizing from app code.
tags: [uno-testing, window-size]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform page switches layout at 720 px wide (VisualStateManager with an AdaptiveTrigger): below that the details pane collapses and a "Show details" button appears. With the Uno App MCP connected to the desktop app, how should my agent check both the narrow and the wide layout and capture screenshots of each for the PR? Describe the calls; don't run them.
