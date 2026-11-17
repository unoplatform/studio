---
description: Sign-out and an expired session both return to login through one app-lifetime LoggedOut handler that clears the back stack.
tags: [uno-authentication, flow]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app was created from the recommended template with custom authentication. Signing in works and takes the user to MainPage. But when the user taps Sign out on MainPage, `LogoutAsync` runs and they stay on MainPage. And when someone leaves the app open until the session expires, API calls start failing and they are never sent back to the login page. How should sign-out and an expired session bring the user back to login?
