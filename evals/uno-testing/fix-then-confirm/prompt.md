---
description: After fixing a failed check in a running app, the agent saves the file and confirms through Hot Reload status, re-asserts, and restarts only when the status says so.
tags: [uno-testing, hot-reload]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My agent is testing my Uno Platform app through the Uno App MCP (desktop, Debug). One check failed: the Save button stays disabled after the form is valid. The cause is a wrong condition in the page model's C# and a typo in a TextBlock in the page's XAML. Once the agent has edited both files, what should it do to confirm the fix in the app? Should it rebuild and relaunch? Walk me through it; don't run anything.
