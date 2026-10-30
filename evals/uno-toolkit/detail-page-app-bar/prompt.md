---
description: A Toolkit NavigationBar handles back navigation on its own; the agent does not wire a back command into MainCommand (which would navigate back twice).
tags: [uno-toolkit, navigationbar]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app the product detail page needs a top bar: the product name as the title, a back arrow that returns to the product list, and a Share button on the right that runs `ShareCommand` from my model. Show me the XAML for the bar.
