---
description: A destructive button and an error-state field in a Simple-theme app use the Simple danger/error styles or error-role brushes, not hard-coded red.
tags: [uno-themes, simple-styles]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Uno Platform app, created with `dotnet new unoapp -preset recommended`, so it's on the default theme. On the account settings page I need a "Delete account" button that clearly reads as destructive, and the confirm-email TextBox above it should show an error look when the email doesn't match. What XAML should I use?
