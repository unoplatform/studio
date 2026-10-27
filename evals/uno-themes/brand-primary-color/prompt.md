---
description: Rebranding a Material app to a brand color overrides the palette through the theme's ThemeColors (seed or override dictionary), not individual controls.
tags: [uno-themes, palette]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app uses the Material theme (`<MaterialTheme />` in App.xaml). Marketing wants our brand blue #0F62FE everywhere the app currently uses its purple accent: filled buttons, switches, checkboxes, selected tabs, links, and it has to look right in dark mode too. Same for errors: validation messages and destructive buttons should use our brand red #DA1E28 instead of the default red. What's the cleanest way to do this without touching every page?
