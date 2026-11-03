---
description: Verifying a Release build, where the App MCP is unavailable, drives the app through the OS accessibility tree (UI Automation on Windows, uiautomator on Android) using AutomationProperties.
tags: [uno-testing, without-app-mcp]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Before we ship, we need an automated smoke test of the Release build of our Uno Platform app (Skia renderer) on Windows desktop and on an Android device: log in with a test account, check the dashboard shows the user's name. The Uno App MCP only works with Debug builds, so that's out. How should we find the controls, drive them, and assert on each platform, and what do we need to change in the XAML so it works?
