---
name: uno-testing
description: "Automated UI testing and verification of a running Uno Platform app through the Uno App MCP (the uno_app_* tools): start the app, take a visual-tree snapshot, find elements by x:Name or automation name, click, type, press keys, scroll, drive navigation flows, capture screenshots, read an element's DataContext, and assert on text, visibility, enabled state, list counts, dialogs, layout, and FeedView loading/error/value states. Use whenever the user says check that, verify, test the UI, does it render, click through, make sure the button works, take a screenshot, why is X not visible, or asks for end-to-end or visual regression testing of an Uno app. Also use after any UI change you made when the uno_app_* tools are available, so the result is confirmed in the real app rather than assumed from the code. Before verifying a Release build, where the App MCP is not available, load this skill: it gives the steps for that path."
compatibility: The Uno App MCP server (the uno_app_* tools, provided by the Uno tooling) for Debug builds on desktop (Windows, macOS, Linux), WebAssembly, iOS, and Android. Without it, Release builds on Windows and Android are verified through the platform accessibility tree.
metadata:
  author: uno-platform
  category: testing
---

# Uno Testing

The Uno App MCP lets you drive the real running app: launch it, read its visual tree, act on elements through automation peers, and check what actually rendered. Use it to prove a change works instead of inferring from source. The failure modes here are mostly timing and stale references, so the rules below are about when to refresh and when to wait.

## Prerequisite

Confirm the app tools exist before starting. Some clients list the `uno_app_*` tools directly; others list only `uno_health`, `uno_discover_tools`, and `uno_execute_tool`. In that case call `uno_discover_tools` to see the app tools and run each one as `uno_execute_tool(toolName: "uno_app_start", arguments: {...})`. If `uno_health` reports `toolCount: 0` or `issues`, or a tool a step names is not offered, read `references/availability.md` before concluding anything. When the tools stay unavailable, and for Release builds, verify through the platform accessibility tree on Windows or Android (`references/without-app-mcp.md`); only where that is not possible either, fall back to static review of the XAML and Model code, labelled as unverified. Do not describe test results you did not obtain.

## Workflow

Verify behaviour on a Debug build with the App MCP. Use a Release build only when the task requires one (a spec that asks for it, or a Release-only defect such as trimming or AOT), and then drive it through the accessibility tree (`references/without-app-mcp.md`) instead of repeating every check with screenshots.

1. **Build, then start.** Make sure Hot Design will not open over the app (see the first critical rule). Build once with `dotnet build -f net10.0-desktop` (or the app's own TFM, read from `<TargetFrameworks>`) so compile errors surface as build output. Then `uno_app_start(projectPath, targetFramework, connectionTimeoutSeconds: 120)` with the full `.csproj` path. Skip the start if the user already has it running.
2. **Wait for the connection; do not restart.** If the start times out, the app is usually still launching. Poll `uno_app_get_runtime_info` every few seconds and call `uno_devserver_diagnostics` if it keeps reporting no instance. Calling `uno_app_start` again kills the process and starts over.
3. **Baseline.** `uno_app_visualtree_snapshot(detail: "normal")` plus a screenshot saved with `path` before touching anything, so failures have a before/after.
4. **Locate.** Each snapshot line is `Type ^handle #Name :line [patterns] "text"`. Find the target by `#Name` (its `x:Name` or `AutomationProperties.Name`), then by its text, then by type and position. The handle is the bare token after `^`: for `Button ^5k #Save`, pass `elementRef: "5k"`, not `"^5k"` (a leading `^` is stripped with a warning).
5. **Act.** `uno_app_element_peer_default_action(elementRef)` always runs `invoke`, so use it only on `[i]` elements (`Button`, `HyperlinkButton`); on anything else it fails. For the other letters use `uno_app_element_peer_action(elementRef, action)`: `t` toggle, `x` expand/collapse, `s` select, `v` setValue (fills a `TextBox` without keyboard focus), `r` setRangeValue. `ListViewItem` shows no letters on Uno, so select it by pointer or keyboard. Without `uno_app_element_peer_action` (Community), take an unscoped `detail: "full"` snapshot, `uno_app_pointer_click` the centre of the element's `@@x,y,w,h` bounds (that focuses a field or selects an item), then `uno_app_type_text(text, intervalInMs: 50)`; or Tab into the field with `uno_app_key_press`.
6. **Wait, then re-snapshot.** Poll the tree until loading indicators are gone or the expected element appears (about 10 tries, 500 ms apart). Handles from the previous snapshot are invalid after UI changes.
7. **Assert.** Check presence and absence in the tree and the text it shows. The tree shows bindings (`Text={Title}`), not current property values, so read state such as `IsEnabled`, `IsChecked`, or a count with `uno_app_get_element_datacontext(elementRef)` or from a screenshot. Without that tool (Community), use the screenshot and the tree's text, and report DataContext assertions as not run. Save a screenshot of the final state.
8. **Fix, then confirm in the running app.** When a check fails and you edit an existing `.xaml` or `.cs` file, Hot Reload applies the edit to the running app within a few seconds (a C# edit recreates the current page, so navigate back to the state under test). This App MCP has no tool that reports the outcome, so wait a few seconds, re-snapshot, and go back to step 6. If the change is still missing after about 10 s, or the edit added a file, changed the `.csproj` or packages, or touched code that already ran at startup (`OnLaunched`, host configuration, DI registrations), call `uno_app_start` again: it terminates the running instance and rebuilds, and reports a failed build with its output.
9. **Report and clean up.** State each assertion with expected vs actual, attach screenshots, and `uno_app_close` on desktop unless the user wants the app left open. On failure, also capture a `detail: "full"` tree and the DataContext.

## Topic map

| Task | Read |
|------|------|
| Tool catalogue, parameters, lifecycle, and the four base test patterns (click, form, navigation, visual regression) | `references/ui.md` |
| Assertion patterns: page loaded, button state, text, list counts, dialogs, validation errors, navigation, wait/retry loops, failure diagnostics | `references/assertions.md` |
| Interaction recipes: text entry and clearing, keyboard navigation, lists and multi-select, ComboBox, sliders, dialogs, scrolling, context menus | `references/INTERACTION-PATTERNS.md` |
| Reading the snapshot text format, `detail`/`includeHidden`/`elementRef`, element-finding strategies, recycled list handles | `references/VISUAL-TREE-GUIDE.md` |
| `uno_health` problems, missing app tools, licensing tiers and the Community fallbacks | `references/availability.md` |
| Release builds, CI, or no App MCP: what Windows UI Automation and Android `uiautomator` expose, finding, acting and asserting through them | `references/without-app-mcp.md` |

## Critical rules

- **Accessibility first, on every path.** Find controls in the element tree (the App MCP snapshot, or Windows UI Automation and Android `uiautomator` without it), act on them through their name, and assert on what the tree reports. A screenshot is a visual assertion or evidence, never the way to find a control or confirm its state while the tree can.
- **Keep Studio's own UI out of the app.** Before the first `uno_app_start`, `App.xaml.cs` should call `MainWindow.UseStudio(showHotReloadIndicator: false, launchHotDesignOnStart: false);` under `#if DEBUG`. With no arguments the Hot Reload indicator and the Hot Design button sit over the app in every screenshot and under pointer clicks, and Hot Design can open over the app on launch: screenshots then show the designer, clicks land on it, and the visual tree does not show it. Hot Reload and the App MCP keep working with the arguments. In an app you created in this session, make the change and mention it in your report. In an existing app, ask the developer first; if they decline, check the first screenshot for the designer, ask them to exit Hot Design if it is there, and report any check it blocked as not run. If Hot Design still opens, the `UNO_HOTDESIGN_LAUNCH` environment variable or the `UnoHotDesignLaunch` MSBuild property is set, and takes precedence.
- **One app instance, and close it before you build.** A running app locks its `bin` folder, so `dotnet build` fails with `MSB3026`/`MSB3027` copy errors while it runs: `uno_app_close` first. `uno_app_start` terminates the instance it started before it builds, so it needs no close. Do not launch the app yourself with `dotnet run` or a script while the App MCP drives it.
- **Save screenshots to files; read one back only for a visual assertion.** Without `path` the image comes back in the tool result and stays in context for every later call. Save one before and after, on pass and on fail: that evidence makes the report reviewable and regressions diagnosable. To save a file, the path must be inside the App MCP's workspace, which is the selected solution's folder (`effectiveWorkspaceDirectory` in `uno_health`), and if the save fails with "Could not find a part of the path", create the folder and retry. Copy the file elsewhere afterwards if the task needs it there.
- **Handles expire.** Any action that changes UI invalidates earlier handles; always re-snapshot before the next interaction. Recycled `ListView` items change handles on scroll.
- **Never assert during a transient state.** A `ProgressRing`, `ProgressBar`, loading text, or disabled submit button means the async work is still running; poll until it clears, then assert. Asserting early produces false failures and false passes.
- **Peers before coordinates.** Automation peers survive layout changes and DPI differences; coordinate clicks do not. When coordinates are unavoidable, take a `detail: "full"` snapshot without `elementRef` and click the centre of the element's `@@x,y,w,h` bounds: the bounds are relative to the snapshot root, and clicks are relative to the window.
- **Name the elements you test.** `#Name` shows `x:Name` or `AutomationProperties.Name`; an element with neither is found only by text or position. For Release verification, set `AutomationProperties.AutomationId` (UI Automation `AutomationId`) and a readable `AutomationProperties.Name`, unique on its page (UI Automation `Name`, Android `content-desc`) on every control a test touches: Android exposes only the name. In an app you created, add them; in an existing app, suggest them.
- **Keep the default `detail: "compact"` or `"normal"`.** `"full"` adds framework nodes, bounds, and offscreen flags and is large; use it for coordinates and failure diagnosis. `includeHidden: true` only proves something is collapsed; never interact with a hidden element.
- **Be specific.** Assert exact text, counts, and enabled states rather than "element exists". Absence is asserted with `includeHidden: false`.
- **Feed states are three assertions, not one.** For a `FeedView`, check the progress template appears, then the value or error template replaces it, then the bound data matches via DataContext.

## Related skills

- `uno-mvux` for what the loading, error, and value states of a `FeedView` should contain and how the ViewModel exposes them.
- `uno-navigation` for the routes and regions a navigation test should land on.
- `uno-toolkit` for the control types (`TabBar`, `NavigationBar`, `CardContentControl`) you will see in the visual tree.
