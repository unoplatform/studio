---
description: After a successful login the app navigates to the home page so that Back cannot return to the login page.
tags: [uno-navigation, qualifiers]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Uno Platform app built from the recommended template, with Uno.Extensions navigation. It starts on a LoginPage; after the user signs in, the LoginModel navigates to MainPage. Problem: on Android, pressing the hardware back button on MainPage takes the user back to the login screen. How do I fix the navigation after login so that can't happen?
