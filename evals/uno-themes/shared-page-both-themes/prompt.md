---
description: XAML shared between a Material app and a Simple app uses the unprefixed semantic style and typography keys, not theme-prefixed ones.
tags: [uno-themes, semantic-styles]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
We have two Uno Platform apps: one uses Uno Material, the other uses the Simple theme. They share a class library of pages. I need a "Edit profile" page in that library: a page title, a paragraph of helper text, a name TextBox, two buttons, a prominent Save and a less prominent Cancel, and at the bottom a red "Delete profile" button. It has to look native in both apps without any per-app XAML. Write the page XAML.
