---
description: Loading, error and retry UI for an async value uses FeedView templates, with a retry button that can reach FeedView's Refresh from the error template.
tags: [uno-mvux, feedview]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app the weather page's model exposes the current forecast, loaded from a weather service. I need a spinner while it loads and, when the request fails (for example offline), a friendly message with a Retry button that loads it again. Show the XAML and any model changes.
