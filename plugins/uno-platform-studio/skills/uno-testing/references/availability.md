# App MCP availability

What to do when `uno_health` reports a problem, or a tool a step needs is not offered.

## `uno_health` reports `toolCount: 0` or `issues`

Read its `issues` and `resolutionKind` before concluding anything. In `issues[].code`, `HostUnreachable` means the DevServer host is still connecting (wait a few seconds and call it again), and `NoSolutionFound` (`resolutionKind: NoCandidates`), `WorkspaceAmbiguous` (`Ambiguous`; `uno_app_select_solution` picks one of `candidateSolutions`) or `WorkspaceNotResolved` (`NoValidWorkspace`: the solutions found have no `global.json` naming Uno.Sdk) means no Uno solution was found or selected under the workspace root (or call `uno_app_initialize(workspaceDirectory, solutionPath)`). Only when the host is connected and discovery still returns no `uno_app_*` tools are the app tools not licensed on this machine: then tell the user to sign in to Uno Platform (the Uno Platform Studio window in their IDE). Licensing is per tool: a Community licence has a non-zero `toolCount` but lacks `uno_app_element_peer_action` and `uno_app_get_element_datacontext` (Pro or Business) and `uno_app_get_memory_counters` (Business); when discovery does not list them, use the fallbacks below and report DataContext assertions as not run.

## An App MCP without `until`, `uno_app_scroll_into_view` or `uno_app_resize`

App MCPs before Uno.UI.App.Mcp 2.0.0-dev.16 (issues unoplatform/uno.app-mcp#210 to #212, shipped by unoplatform/uno.app-mcp#214) lack them.

- **Waiting for a state.** `uno_app_find_elements(waitForMs)` waits only for a match. Wait for the element that appears with the new state when there is one (the next page's first item, a confirmation text); otherwise read the state once, and when two reads in a row return the same state, read the DataContext or the app log rather than a third time.
- **Reaching a list item.** Focus the list and press `PageDown` or `End` with `uno_app_key_press`, then `uno_app_find_elements` with `waitForMs` on the item or the list's `{items=N}`.
- **Window size.** Read the size from a launch argument in `OnLaunched` and apply it with `MainWindow.AppWindow.Resize(new SizeInt32(width + frameWidth, height + frameHeight))`, where the frame is `AppWindow.Size` minus the client size (`ResizeClient` is not implemented in Uno); pass the size through `uno_app_start(args)`.

## Community licence fallbacks

- **Acting without `uno_app_element_peer_action`.** Take an unscoped `detail: "full"` snapshot, `uno_app_pointer_click` the centre of the element's `@@x,y,w,h` bounds (that focuses a field or selects an item), then `uno_app_type_text(text, intervalInMs: 50)`; or Tab into the field with `uno_app_key_press`.
- **Reading values without `uno_app_get_element_datacontext`.** Use the screenshot and the tree's text, and report DataContext assertions as not run.
