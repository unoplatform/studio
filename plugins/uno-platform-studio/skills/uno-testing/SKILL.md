---
name: uno-testing
description: "Automated UI testing and verification of a running Uno Platform app through the Uno App MCP (the uno_app_* tools): start the app, take a visual-tree snapshot, find elements by x:Name or automation name, click, type, press keys, scroll, drive navigation flows, capture screenshots, read an element's DataContext, and assert on text, visibility, enabled state, list counts, dialogs, layout, and FeedView loading/error/value states. Use whenever the user says check that, verify, test the UI, does it render, click through, make sure the button works, take a screenshot, why is X not visible, or asks for end-to-end or visual regression testing of an Uno app. Also use after any UI change you made when the uno_app_* tools are available, so the result is confirmed in the real app rather than assumed from the code."
compatibility: Requires the Uno App MCP server (the uno_app_* tools, provided by the Uno tooling) to be configured and available. Works with Uno Platform applications targeting desktop (Windows, macOS, Linux), WebAssembly, iOS, and Android.
metadata:
  author: uno-platform
  version: "3.0"
  category: testing
---

# Uno Testing

The Uno App MCP lets you drive the real running app: launch it, read its visual tree, act on elements through automation peers, and check what actually rendered. Use it to prove a change works instead of inferring from source. The failure modes here are mostly timing and stale references, so the rules below are about when to refresh and when to wait.

## Prerequisite

Confirm the app tools exist before starting. Some clients list the `uno_app_*` tools directly; others list only `uno_health`, `uno_discover_tools`, and `uno_execute_tool`. In that case call `uno_discover_tools` to see the app tools and run each one as `uno_execute_tool(toolName: "uno_app_start", arguments: {...})`. If `uno_health` reports `toolCount: 0`, read its `issues` and `resolutionKind` before concluding anything. In `issues[].code`, `HostUnreachable` means the DevServer host is still connecting (wait a few seconds and call it again), and `NoSolutionFound` or `WorkspaceAmbiguous` means no Uno solution was found or selected under the workspace root (`resolutionKind` then reads `NoCandidates`, `NoValidWorkspace`, or `Ambiguous`; `uno_app_select_solution` picks one of `candidateSolutions`, or call `uno_app_initialize(workspaceDirectory, solutionPath)`). Only when the host is connected and discovery still returns no `uno_app_*` tools are the app tools not licensed on this machine: then tell the user to sign in to Uno Platform (the Uno Platform Studio window in their IDE). Licensing is per tool: a Community licence has a non-zero `toolCount` but lacks `uno_app_element_peer_action` and `uno_app_get_element_datacontext` (Pro or Business) and `uno_app_get_memory_counters` (Business); when discovery does not list them, use the fallbacks named in the steps below and report DataContext assertions as not run. In every case where the tools stay unavailable, fall back to static review of the XAML and Model code, labelled as unverified. Do not describe test results you did not obtain.

## Workflow

1. **Build, then start.** Make sure Hot Design will not open over the app (see the first critical rule). Build once with `dotnet build -f net10.0-desktop` (or the app's own TFM, read from `<TargetFrameworks>`) so compile errors surface as build output. Then `uno_app_start(projectPath, targetFramework, connectionTimeoutSeconds: 120)` with the full `.csproj` path. Skip the start if the user already has it running.
2. **Wait for the connection; do not restart.** If the start times out, the app is usually still launching. Poll `uno_app_get_runtime_info` every few seconds and call `uno_devserver_diagnostics` if it keeps reporting no instance. Calling `uno_app_start` again kills the process and starts over.
3. **Baseline.** `uno_app_get_screenshot` plus `uno_app_visualtree_snapshot(detail: "normal")` before touching anything, so failures have a before/after.
4. **Locate.** Each snapshot line is `Type ^handle #Name :line [patterns] "text"`. Find the target by `#Name` (its `x:Name` or `AutomationProperties.Name`), then by its text, then by type and position. The handle is the bare token after `^`: for `Button ^5k #Save`, pass `elementRef: "5k"`, never `"^5k"`.
5. **Act.** Prefer `uno_app_element_peer_default_action(elementRef)`. For anything else use `uno_app_element_peer_action(elementRef, action)` with an action the line's pattern letters allow: `i` invoke, `t` toggle, `x` expand/collapse, `s` select, `v` setValue (fills a `TextBox` without keyboard focus), `r` setRangeValue. Without `uno_app_element_peer_action` (Community), focus the field with its default action and then `uno_app_type_text`. Use `uno_app_type_text` and `uno_app_key_press` for real keyboard input, and `uno_app_pointer_click` only when no pattern applies (bounds come from `detail: "full"`).
6. **Wait, then re-snapshot.** Poll the tree until loading indicators are gone or the expected element appears (about 10 tries, 500 ms apart). Handles from the previous snapshot are invalid after UI changes.
7. **Assert.** Check presence and absence in the tree and the text it shows. The tree shows bindings (`Text={Title}`), not current property values, so read state such as `IsEnabled`, `IsChecked`, or a count with `uno_app_get_element_datacontext(elementRef)` or from a screenshot. Without that tool (Community), use the screenshot and the tree's text, and report DataContext assertions as not run. Screenshot the final state.
8. **Report and clean up.** State each assertion with expected vs actual, attach screenshots, and `uno_app_close` on desktop unless the user wants the app left open. On failure, also capture a `detail: "full"` tree and the DataContext.

## Topic map

| Task | Read |
|------|------|
| Tool catalogue, parameters, lifecycle, and the four base test patterns (click, form, navigation, visual regression) | `references/ui.md` |
| Assertion patterns: page loaded, button state, text, list counts, dialogs, validation errors, navigation, wait/retry loops, failure diagnostics | `references/assertions.md` |
| Interaction recipes: text entry and clearing, keyboard navigation, lists and multi-select, ComboBox, sliders, dialogs, scrolling, context menus | `references/INTERACTION-PATTERNS.md` |
| Reading the snapshot text format, `detail`/`includeHidden`/`elementRef`, element-finding strategies, recycled list handles | `references/VISUAL-TREE-GUIDE.md` |

## Critical rules

- **Keep Hot Design from opening over the app.** Uno templates call `MainWindow.UseStudio();` under `#if DEBUG` in `App.xaml.cs`. With no argument, Hot Design opens by itself on the app's first launch, and on any launch after a session where it was active. Its designer and introduction overlay then cover the app: screenshots show the designer, pointer clicks land on it, and the visual tree does not show it. Before the first `uno_app_start`, the call should be `MainWindow.UseStudio(launchHotDesignOnStart: false);`, still under `#if DEBUG`; Hot Reload and the Hot Design button stay available. In an app you created in this session, make the change and mention it in your report. In an existing app, ask the developer first, because it changes how their app starts under the debugger. If they decline, leave the call as it is, check the first screenshot for the Hot Design designer, and if it is there ask them to exit Hot Design in the app window; report any runtime check it blocked as not run. If Hot Design still opens, the `UNO_HOTDESIGN_LAUNCH` environment variable or the `UnoHotDesignLaunch` MSBuild property is set: both take precedence over the argument.
- **One app instance, and close it before you build.** A running app locks its `bin` folder, so `dotnet build` or another `uno_app_start` fails with `MSB3026`/`MSB3027` copy errors. `uno_app_close` first, and do not launch the app yourself with `dotnet run` or a script while the App MCP drives it.
- **Screenshots return an image by default; a `path` has rules.** Omit `path` to get the image back in the tool result. To save a file, the path must be inside the App MCP's workspace, which is the selected solution's folder (`effectiveWorkspaceDirectory` in `uno_health`), and if the save fails with "Could not find a part of the path", create the folder and retry. Copy the file elsewhere afterwards if the task needs it there.
- **Handles expire.** Any action that changes UI invalidates earlier handles; always re-snapshot before the next interaction. Recycled `ListView` items change handles on scroll.
- **Never assert during a transient state.** A `ProgressRing`, `ProgressBar`, loading text, or disabled submit button means the async work is still running; poll until it clears, then assert. Asserting early produces false failures and false passes.
- **Peers before coordinates.** Automation peers survive layout changes and DPI differences; coordinate clicks do not. When coordinates are unavoidable, take a `detail: "full"` snapshot and click the centre of the element's `@@x,y,w,h` bounds.
- **Name the elements you test.** `#Name` shows `x:Name` or `AutomationProperties.Name`; an element with neither is found only by text or position. If the test matters, suggest adding `x:Name` or `AutomationProperties.Name` to the XAML.
- **Keep the default `detail: "compact"` or `"normal"`.** `"full"` adds framework nodes, bounds, and offscreen flags and is large; use it for coordinates and failure diagnosis. `includeHidden: true` only proves something is collapsed; never interact with a hidden element.
- **Be specific.** Assert exact text, counts, and enabled states rather than "element exists". Absence is asserted with `includeHidden: false`.
- **Screenshot before and after.** Evidence on both pass and fail is what makes the report reviewable and makes regressions diagnosable later.
- **Feed states are three assertions, not one.** For a `FeedView`, check the progress template appears, then the value or error template replaces it, then the bound data matches via DataContext.

## Related skills

- `uno-mvux` for what the loading, error, and value states of a `FeedView` should contain and how the ViewModel exposes them.
- `uno-navigation` for the routes and regions a navigation test should land on.
- `uno-toolkit` for the control types (`TabBar`, `NavigationBar`, `CardContentControl`) you will see in the visual tree.
