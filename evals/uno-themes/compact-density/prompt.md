---
description: Tightening spacing app-wide on a recommended-template app sets DefaultDensity on the existing SimpleToolkitTheme instead of editing margins page by page.
tags: [uno-themes, density]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I built a back-office data-entry tool with Uno Platform (`dotnet new unoapp -preset recommended`). Users on desktop say everything is too spaced out: buttons, text boxes, list rows all have generous padding and they want to see more on screen. Is there a way to tighten the whole app at once instead of going through every page's margins and paddings?
