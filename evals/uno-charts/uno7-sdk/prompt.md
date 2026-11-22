---
description: On Uno.Sdk 7.x the agent installs LiveCharts2 2.2.0, the first build for Uno 7, not 2.0.5, and does not suppress UNOB0023 or force dependency versions.
tags: [uno-charts, uno-sdk-7]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app's global.json pins `"Uno.Sdk": "7.0.22"`. I want a line chart of daily active users on the home page, from the `IListFeed<DailyCount>` the HomeModel already has. Which charting library should I add, and how do I set it up?
