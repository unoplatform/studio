---
description: Card-shaped list items are written as a Toolkit CardContentControl bound to the item, not a rounded Border with a shadow or the slot-based Card.
tags: [uno-toolkit, card]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (Material theme) shows a list of recipes in a ListView bound to `Recipes`. Each item should look like a card: rounded corners, a subtle shadow, the photo on top, then the title and cook time underneath. What should the ItemTemplate XAML look like?
