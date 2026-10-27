---
description: Running a model command when a ListView row is tapped uses Toolkit CommandExtensions.Command with IsItemClickEnabled, not an ItemClick/SelectionChanged handler in code-behind.
tags: [uno-toolkit, command-extensions]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform orders page shows a ListView bound to `Orders`, and the page model (MVUX) has a `public async ValueTask OpenOrder(Order order)` method. When the user taps a row, I want OpenOrder called with that order. I'd like to avoid code-behind event handlers and keep the ListView from leaving a row selected. What XAML do I need?
