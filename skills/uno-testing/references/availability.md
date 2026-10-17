# App MCP availability

What to do when `uno_health` reports a problem, or a tool a step needs is not offered.

## `uno_health` reports `toolCount: 0` or `issues`

Read its `issues` and `resolutionKind` before concluding anything. In `issues[].code`, `HostUnreachable` means the DevServer host is still connecting (wait a few seconds and call it again), and `NoSolutionFound` (`resolutionKind: NoCandidates`), `WorkspaceAmbiguous` (`Ambiguous`; `uno_app_select_solution` picks one of `candidateSolutions`) or `WorkspaceNotResolved` (`NoValidWorkspace`: the solutions found have no `global.json` naming Uno.Sdk) means no Uno solution was found or selected under the workspace root (or call `uno_app_initialize(workspaceDirectory, solutionPath)`). Only when the host is connected and discovery still returns no `uno_app_*` tools are the app tools not licensed on this machine: then tell the user to sign in to Uno Platform (the Uno Platform Studio window in their IDE). Licensing is per tool: a Community licence has a non-zero `toolCount` but lacks `uno_app_element_peer_action` and `uno_app_get_element_datacontext` (Pro or Business) and `uno_app_get_memory_counters` (Business); when discovery does not list them, use the fallbacks below and report DataContext assertions as not run.

## Community licence fallbacks

- **Acting without `uno_app_element_peer_action`.** Take an unscoped `detail: "full"` snapshot, `uno_app_pointer_click` the centre of the element's `@@x,y,w,h` bounds (that focuses a field or selects an item), then `uno_app_type_text(text, intervalInMs: 50)`; or Tab into the field with `uno_app_key_press`.
- **Reading values without `uno_app_get_element_datacontext`.** Use the screenshot and the tree's text, and report DataContext assertions as not run.
