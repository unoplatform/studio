---
description: Checking a FeedView-backed list waits for the loading state to clear and asserts the exact item count from the live tree, not from an early snapshot.
tags: [uno-testing, assertions]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I use the Uno App MCP so my coding agent can check my Uno Platform app while it runs. The Orders page has an MVUX FeedView that loads orders from our API (a ProgressRing shows while it loads) into a ListView. I asked the agent to confirm the page shows 12 orders after launch; it took a visual tree snapshot right after the page opened, said the list was empty, and called it a bug, but when I looked the 12 orders were there. Don't run anything: tell me how that check should have been done, step by step, with the calls it should make.
