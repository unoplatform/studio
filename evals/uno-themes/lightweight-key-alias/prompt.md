---
description: Pointing a control's lightweight styling key at a brush the theme already has aliases it once with a StaticResource element, instead of a new SolidColorBrush per theme.
tags: [uno-themes, lightweight-styling]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app is on the Material theme (`<MaterialTheme />` in App.xaml). Design wants every filled button in the app to use the theme's tertiary color instead of the primary one, and outlined buttons to get a primary-colored border instead of the gray outline. It has to keep working in light and dark mode, and if we ever change the palette later the buttons should follow. What do I put in App.xaml?
