---
description: On Uno.Sdk 7.x Mapsui fails with UNOB0023; the agent says so before writing map code and does not suppress the check or force the dependencies.
tags: [uno-maps, uno-sdk-7]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app's global.json pins `"Uno.Sdk": "7.0.22"`. I want a map of our delivery drivers on the dispatch page, from the `IListFeed<Driver>` (with Latitude and Longitude) the DispatchModel already has. Which map library should I add, and how do I set it up?