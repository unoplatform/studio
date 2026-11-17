---
description: Selection on an ItemsRepeater uses the Toolkit ItemsRepeaterExtensions with a SelectionMode and a toggleable item root, not code-behind or a switch to ListView.
tags: [uno-toolkit, itemsrepeater-extensions]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app I render a horizontal row of category pills with an ItemsRepeater bound to `Categories`. I need the user to tap one to select it (it should look selected), and I want the selected category bound to `SelectedCategory` on my model rather than handled in code-behind. How do I do that while keeping the ItemsRepeater?
