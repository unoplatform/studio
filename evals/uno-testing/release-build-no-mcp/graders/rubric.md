---
type: llm
---
The reply explains how to smoke-test a Release build of an Uno Platform (Skia) app on Windows and Android without the Uno App MCP.
Uno facts the judge needs: Uno exposes its automation peers to the OS accessibility tree. On Windows, UI Automation (e.g. `System.Windows.Automation` from PowerShell or .NET) sees `AutomationProperties.AutomationId` as `AutomationId` (falling back to `x:Name`) and `AutomationProperties.Name` as `Name`; act with Invoke/Value/Toggle patterns and read `Name`/`IsEnabled`. On Android, `adb shell uiautomator dump` exposes `AutomationProperties.Name` as `content-desc`; on Uno 6.x the AutomationId is not exposed as `resource-id` (newer 7.0 builds map it, unverified), so controls are found by a unique `AutomationProperties.Name`; tap the centre of a node's `bounds` with `adb shell input tap` and type with `adb shell input text`; re-dump after every action and retry while the UI settles. A TextBlock's current text appears as its name, so the user's name can be asserted from the tree.
PASS if all hold:
- Windows: drive the app through UI Automation, finding controls by AutomationId (or Name), acting through patterns, and asserting on what the tree reports.
- Android: use `uiautomator dump` (directly or through a tool built on it such as Appium's UiAutomator2), find controls by `content-desc` / `AutomationProperties.Name`, and re-read the tree after actions.
- XAML: add `AutomationProperties.AutomationId` and a readable, unique `AutomationProperties.Name` to the controls the test touches, noting that Android relies on the Name.
- Waiting is done by polling the tree, not fixed sleeps alone.
FAIL if it relies on screenshots or pixel matching as the main way to find controls or assert, claims the App MCP works in Release, or says Android finds controls by AutomationId without qualification.
