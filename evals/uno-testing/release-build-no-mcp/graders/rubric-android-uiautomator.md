---
type: llm
---
The reply explains how to smoke-test a Release build of an Uno Platform (Skia) app on Windows and Android without the Uno App MCP.
Facts the judge needs: On Android, `adb shell uiautomator dump` exposes `AutomationProperties.Name` as `content-desc`; on Uno 6.x the AutomationId is not exposed as `resource-id` (newer 7.0 builds map it, unverified), so controls are found by a unique `AutomationProperties.Name`; tap the centre of a node's `bounds` with `adb shell input tap` and type with `adb shell input text`; re-dump after every action.
PASS if on Android it uses `uiautomator dump` (directly or through a tool built on it such as Appium's UiAutomator2), finds controls by `content-desc` / `AutomationProperties.Name`, and re-reads the tree after actions.
FAIL if it says Android finds controls by AutomationId without qualification, or relies on screenshots or pixel matching on Android.
