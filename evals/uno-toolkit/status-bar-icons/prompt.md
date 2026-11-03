---
description: Per-page status bar colors use the Toolkit StatusBar attached properties, with Foreground as an enum value (not a brush) and the iOS Info.plist prerequisite.
tags: [uno-toolkit, statusbar]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
The profile page in my Uno Platform app has a dark navy header that runs up to the top of the screen. On iOS and Android the status bar icons (clock, battery) stay black and disappear against it. On that page only, I want white status bar icons and the status bar background to be navy. How do I set that up?
