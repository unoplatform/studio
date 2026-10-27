---
description: Adding a Toolkit tab bar with navigation to a blank-preset Uno app goes through UnoFeatures and does not use theme style keys the Fluent default lacks.
tags: [uno-platform, project]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I made an Uno Platform app a while back with `dotnet new unoapp -preset blank`. Now I want a bottom tab bar (Home, Search, Profile) using the Uno Toolkit TabBar, switching pages with Uno's route-based navigation. What do I need to change in the project, and what does the tab bar XAML look like?
